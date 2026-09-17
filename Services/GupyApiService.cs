using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkIT.App.Models;

namespace WorkIT.App.Services;

public class GupyApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://employability-portal.gupy.io/api/v1/jobs";

    public GupyApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WorkIT-App/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
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
        int offset = 0;
        int paginasConsultadas = 0;

        string estadoNorm = VagaClassifierService.Normalizar(estadoFiltro);
        string cidadeNorm = VagaClassifierService.Normalizar(cidadeFiltro);

        while (paginasConsultadas < maximoPaginas)
        {
            string url = $"{BaseUrl}?jobName={Uri.EscapeDataString(palavraChave)}&limit={limitePorPagina}&offset={offset}";

            GupyApiResponse? response;
            try
            {
                response = await _httpClient.GetFromJsonAsync<GupyApiResponse>(url, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao consultar API da Gupy para '{palavraChave}': {ex.Message}");
                break;
            }

            if (response?.Data == null || response.Data.Count == 0)
            {
                break;
            }

            foreach (var item in response.Data)
            {
                string itemEstadoNorm = VagaClassifierService.Normalizar(item.State);
                string itemCidadeNorm = VagaClassifierService.Normalizar(item.City);

                string itemTituloNorm = VagaClassifierService.Normalizar(item.Name);

                // Filtro de Estado (verifica campo de estado e menções no título)
                if (!string.IsNullOrEmpty(estadoNorm))
                {
                    bool matchEstado = itemEstadoNorm.Contains(estadoNorm) || itemTituloNorm.Contains(estadoNorm);
                    if (!matchEstado)
                    {
                        continue;
                    }
                }

                // Filtro de Cidade (verifica campo de cidade e menções no título)
                if (!string.IsNullOrEmpty(cidadeNorm))
                {
                    bool matchCidade = itemCidadeNorm.Contains(cidadeNorm) || itemTituloNorm.Contains(cidadeNorm);
                    if (!matchCidade)
                    {
                        continue;
                    }
                }

                vagas.Add(new Vaga
                {
                    Id = item.Id,
                    Titulo = item.Name ?? "Vaga sem título",
                    Empresa = item.CareerPageName ?? "Empresa confidencial",
                    Cidade = item.City ?? string.Empty,
                    Estado = item.State ?? string.Empty,
                    Url = item.JobUrl ?? string.Empty,
                    Tipo = item.Type ?? string.Empty,
                    Nivel = VagaClassifierService.DetectarNivel(item.Name),
                    DataPublicacao = item.PublishedDate ?? string.Empty
                });
            }

            paginasConsultadas++;
            int total = response.Pagination?.Total ?? 0;
            offset += limitePorPagina;

            if (offset >= total)
            {
                break;
            }
        }

        return vagas;
    }
}
