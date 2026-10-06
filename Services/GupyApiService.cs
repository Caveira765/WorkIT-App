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
        int limitePorPagina = 50,
        int maximoPaginas = 2,
        CancellationToken cancellationToken = default)
    {
        var vagas = new List<Vaga>();
        string buildId = await ObterBuildIdAsync(cancellationToken);

        string estadoNorm = VagaClassifierService.Normalizar(estadoFiltro);
        string cidadeNorm = VagaClassifierService.Normalizar(cidadeFiltro);

        for (int pagina = 1; pagina <= maximoPaginas; pagina++)
        {
            string url = $"https://portal.gupy.io/_next/data/{buildId}/vagas.json?jobName={Uri.EscapeDataString(palavraChave)}&page={pagina}";

            GupyNextDataResponse? nextData = null;
            try
            {
                nextData = await _httpClient.GetFromJsonAsync<GupyNextDataResponse>(url, cancellationToken);
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Se der 404, o buildId pode ter sido renovado pela Gupy: força renovação e tenta novamente
                _lastBuildIdCheck = DateTime.MinValue;
                buildId = await ObterBuildIdAsync(cancellationToken);
                url = $"https://portal.gupy.io/_next/data/{buildId}/vagas.json?jobName={Uri.EscapeDataString(palavraChave)}&page={pagina}";
                try
                {
                    nextData = await _httpClient.GetFromJsonAsync<GupyNextDataResponse>(url, cancellationToken);
                }
                catch (Exception retryEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro na tentativa com novo buildId: {retryEx.Message}");
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao consultar vagas da Gupy para '{palavraChave}': {ex.Message}");
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

                // Filtro de Estado (verifica campo de estado e menções no título)
                if (!string.IsNullOrEmpty(estadoNorm))
                {
                    bool matchEstado = itemEstadoNorm.Contains(estadoNorm) || itemTituloNorm.Contains(estadoNorm);
                    if (!matchEstado) continue;
                }

                // Filtro de Cidade (verifica campo de cidade e menções no título)
                if (!string.IsNullOrEmpty(cidadeNorm))
                {
                    bool matchCidade = itemCidadeNorm.Contains(cidadeNorm) || itemTituloNorm.Contains(cidadeNorm);
                    if (!matchCidade) continue;
                }

                vagas.Add(new Vaga
                {
                    Id = item.Id,
                    Titulo = item.Name ?? "Vaga sem título",
                    Empresa = item.CareerPageName ?? "Empresa confidencial",
                    Cidade = item.City ?? string.Empty,
                    Estado = item.State ?? string.Empty,
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
