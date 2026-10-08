using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WorkIT.App.Models;

namespace WorkIT.App.Services;

public class GupyApiService
{
    private readonly HttpClient _httpClient;
    private string _cachedBuildId = "cV0_DMl2AbEm4R-kj_nZu";
    private DateTime _lastBuildIdCheck = DateTime.MinValue;

    public GupyApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private async Task<string> ObterBuildIdAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - _lastBuildIdCheck < TimeSpan.FromHours(2) && !string.IsNullOrEmpty(_cachedBuildId))
        {
            return _cachedBuildId;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://portal.gupy.io");
            req.Headers.Add("Accept", "text/html");
            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                string html = await resp.Content.ReadAsStringAsync(cancellationToken);
                var match = Regex.Match(html, "\"buildId\":\"([^\"]+)\"");
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    _cachedBuildId = match.Groups[1].Value;
                    _lastBuildIdCheck = DateTime.UtcNow;
                    return _cachedBuildId;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Aviso ao obter buildId atual da Gupy: {ex.Message}. Usando fallback.");
        }

        return _cachedBuildId;
    }

    public async Task<List<Vaga>> BuscarVagasPorPalavraAsync(
        string palavraChave,
        string? estadoFiltro = null,
        string? cidadeFiltro = null,
        int paginaInicial = 1,
        int quantidadePaginas = 2,
        CancellationToken cancellationToken = default)
    {
        var vagas = new List<Vaga>();
        string buildId = await ObterBuildIdAsync(cancellationToken);

        string estadoNorm = VagaClassifierService.Normalizar(estadoFiltro);
        string cidadeNorm = VagaClassifierService.Normalizar(cidadeFiltro);
        string termoTratado = palavraChave?.Trim() ?? string.Empty;
        string encodedTerm = Uri.EscapeDataString(termoTratado);

        int paginaFinal = paginaInicial + Math.Max(1, quantidadePaginas) - 1;

        for (int pagina = paginaInicial; pagina <= paginaFinal; pagina++)
        {
            // O endpoint direto de pesquisa no portal Gupy é job-search/term={termo}.json
            string url = string.IsNullOrWhiteSpace(encodedTerm)
                ? $"https://portal.gupy.io/_next/data/{buildId}/vagas.json?page={pagina}"
                : $"https://portal.gupy.io/_next/data/{buildId}/job-search/term={encodedTerm}.json?page={pagina}";

            GupyNextDataResponse? nextData = null;
            try
            {
                nextData = await _httpClient.GetFromJsonAsync<GupyNextDataResponse>(url, cancellationToken);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Se der 404, tenta renovar o buildId
                _lastBuildIdCheck = DateTime.MinValue;
                buildId = await ObterBuildIdAsync(cancellationToken);
                url = string.IsNullOrWhiteSpace(encodedTerm)
                    ? $"https://portal.gupy.io/_next/data/{buildId}/vagas.json?page={pagina}"
                    : $"https://portal.gupy.io/_next/data/{buildId}/job-search/term={encodedTerm}.json?page={pagina}";

                try
                {
                    nextData = await _httpClient.GetFromJsonAsync<GupyNextDataResponse>(url, cancellationToken);
                }
                catch
                {
                    // Fallback para endpoint de vagas geral com parâmetro de busca
                    try
                    {
                        string fallbackUrl = $"https://portal.gupy.io/_next/data/{buildId}/vagas.json?jobName={encodedTerm}&page={pagina}";
                        nextData = await _httpClient.GetFromJsonAsync<GupyNextDataResponse>(fallbackUrl, cancellationToken);
                    }
                    catch (Exception exFallback)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro no fallback de vagas: {exFallback.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao consultar vagas para '{palavraChave}' página {pagina}: {ex.Message}");
                break;
            }

            var itens = nextData?.PageProps?.InitialJobList?.Data;
            if (itens == null || itens.Count == 0)
            {
                break;
            }

            foreach (var item in itens)
            {
                string itemEstadoNorm = VagaClassifierService.Normalizar(item.State);
                string itemCidadeNorm = VagaClassifierService.Normalizar(item.City);
                string itemTituloNorm = VagaClassifierService.Normalizar(item.Name);

                bool ehRemoto = item.WorkplaceType?.Equals("remote", StringComparison.OrdinalIgnoreCase) == true
                    || itemCidadeNorm.Contains("remoto")
                    || itemTituloNorm.Contains("remoto")
                    || itemTituloNorm.Contains("home office");

                // Filtro de Estado (casamento de estado OU vaga remota)
                if (!string.IsNullOrEmpty(estadoNorm))
                {
                    bool matchEstado = itemEstadoNorm.Contains(estadoNorm) || itemTituloNorm.Contains(estadoNorm) || ehRemoto;
                    if (!matchEstado) continue;
                }

                // Filtro de Cidade (casamento de cidade OU vaga remota)
                if (!string.IsNullOrEmpty(cidadeNorm))
                {
                    bool matchCidade = itemCidadeNorm.Contains(cidadeNorm) || itemTituloNorm.Contains(cidadeNorm) || ehRemoto;
                    if (!matchCidade) continue;
                }

                vagas.Add(new Vaga
                {
                    Id = item.Id,
                    Titulo = item.Name ?? "Vaga sem título",
                    Empresa = item.CareerPageName ?? "Empresa confidencial",
                    Cidade = item.City ?? (ehRemoto ? "Remoto" : string.Empty),
                    Estado = item.State ?? (ehRemoto ? "Brasil" : string.Empty),
                    Url = string.IsNullOrWhiteSpace(item.JobUrl) ? $"https://portal.gupy.io/vagas/{item.Id}" : item.JobUrl,
                    Tipo = item.Type ?? string.Empty,
                    Nivel = VagaClassifierService.DetectarNivel(item.Name),
                    DataPublicacao = item.PublishedDate ?? string.Empty
                });
            }

            var pagination = nextData?.PageProps?.InitialJobList?.Pagination;
            if (pagination != null && pagination.Offset + pagination.Limit >= pagination.Total)
            {
                break;
            }
        }

        return vagas;
    }
}
