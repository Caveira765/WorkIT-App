using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WorkIT.App.Data;
using WorkIT.App.Models;
using WorkIT.App.Services;

namespace WorkIT.App;

public partial class MainWindow : Window
{
    private readonly GupyApiService _apiService;
    private readonly DatabaseService _databaseService;
    private List<Vaga> _vagasAtuais = new();

    public MainWindow()
    {
        InitializeComponent();

        _apiService = new GupyApiService();
        _databaseService = new DatabaseService();

        InicializarFiltros();
        AtualizarEstatisticasEBanco();
        CarregarVagasFavoritas();
    }

    private void InicializarFiltros()
    {
        CmbArea.ItemsSource = VagaClassifierService.Areas;
        CmbArea.SelectedIndex = 0; // Todas as áreas

        CmbEstado.ItemsSource = VagaClassifierService.Estados;
        CmbEstado.SelectedIndex = 0; // Todos os estados
    }

    private void AtualizarEstatisticasEBanco()
    {
        try
        {
            int totalBanco = _databaseService.ObterTotalNoBanco();
            TxtTotalBanco.Text = totalBanco.ToString();

            var stats = _databaseService.ObterEstatisticasNivel();
            TxtQtdEstagio.Text = stats.TryGetValue("Estágio", out int est) ? est.ToString() : "0";
            TxtQtdJunior.Text = stats.TryGetValue("Júnior", out int jr) ? jr.ToString() : "0";
            TxtQtdPleno.Text = stats.TryGetValue("Pleno", out int pl) ? pl.ToString() : "0";
            TxtQtdSenior.Text = stats.TryGetValue("Sênior", out int sr) ? sr.ToString() : "0";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao atualizar estatísticas: {ex.Message}");
        }
    }

    private void CarregarVagasFavoritas()
    {
        try
        {
            var favoritas = _databaseService.ObterFavoritas();
            ItemsFavoritas.ItemsSource = favoritas;
            EmptyStateFavoritas.Visibility = favoritas.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao carregar favoritas: {ex.Message}");
        }
    }

    private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && BtnBuscar.IsEnabled)
        {
            BtnBuscar_Click(sender, e);
        }
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        BtnBuscar.IsEnabled = false;
        BtnExportar.IsEnabled = false;
        ProgressBusca.Visibility = Visibility.Visible;
        TxtStatusRodape.Text = "Consultando oportunidades na API pública da Gupy...";

        var areaSelecionada = (AreaModel)CmbArea.SelectedItem;
        var estadoSelecionado = (EstadoModel)CmbEstado.SelectedItem;
        string cidade = TxtCidade.Text.Trim();
        string termoExtra = TxtTermoExtra.Text.Trim();

        string? filtroEstado = string.IsNullOrWhiteSpace(estadoSelecionado?.Nome) || estadoSelecionado.Sigla == ""
            ? null
            : estadoSelecionado.Nome;

        string? filtroCidade = string.IsNullOrWhiteSpace(cidade) ? null : cidade;

        List<string> termosDeBusca = new();

        if (!string.IsNullOrWhiteSpace(termoExtra))
        {
            termosDeBusca.Add(termoExtra);
        }

        if (areaSelecionada != null)
        {
            if (areaSelecionada.Id == "0") // Todas as áreas
            {
                if (termosDeBusca.Count == 0)
                {
                    termosDeBusca.AddRange(new[] { "desenvolvedor", "programador", "software", "analista ti" });
                }
            }
            else
            {
                // Pega as 3 primeiras palavras-chave da área para uma busca rápida e relevante
                termosDeBusca.AddRange(areaSelecionada.PalavrasChave.Take(3));
            }
        }

        if (termosDeBusca.Count == 0)
        {
            termosDeBusca.Add("desenvolvedor");
        }

        try
        {
            var vagasMapeadas = new Dictionary<long, Vaga>();

            await Task.Run(async () =>
            {
                foreach (var termo in termosDeBusca)
                {
                    var resultados = await _apiService.BuscarVagasPorPalavraAsync(
                        termo,
                        filtroEstado,
                        filtroCidade,
                        limitePorPagina: 50,
                        maximoPaginas: 1
                    );

                    foreach (var v in resultados)
                    {
                        if (!vagasMapeadas.ContainsKey(v.Id))
                        {
                            v.Area = areaSelecionada?.Nome ?? "Geral";
                            vagasMapeadas[v.Id] = v;
                        }
                    }
                }
            });

            _vagasAtuais = vagasMapeadas.Values.ToList();

            // Salva no banco SQLite e verifica novidade
            int novas = 0;
            int jaVistas = 0;

            foreach (var vaga in _vagasAtuais)
            {
                bool ehNova = _databaseService.SalvarVaga(vaga);
                if (ehNova) novas++;
                else jaVistas++;
            }

            // Atualiza UI
            ItemsVagas.ItemsSource = null;
            ItemsVagas.ItemsSource = _vagasAtuais;
            EmptyStatePlaceholder.Visibility = _vagasAtuais.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            TxtTotalEncontradas.Text = _vagasAtuais.Count.ToString();
            TxtTotalNovas.Text = novas.ToString();
            TxtTotalJaVistas.Text = jaVistas.ToString();

            TxtStatusLista.Text = $"Foram encontradas {_vagasAtuais.Count} oportunidades ({novas} novas identificadas e adicionadas ao banco local).";
            TxtStatusRodape.Text = $"Busca concluída: {_vagasAtuais.Count} vagas encontradas ({novas} novas, {jaVistas} já catalogadas).";

            AtualizarEstatisticasEBanco();
            CarregarVagasFavoritas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao buscar vagas: {ex.Message}", "Erro de Comunicação", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatusRodape.Text = "Erro durante a consulta na API.";
        }
        finally
        {
            BtnBuscar.IsEnabled = true;
            BtnExportar.IsEnabled = true;
            ProgressBusca.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnAbrirLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Vaga vaga && !string.IsNullOrWhiteSpace(vaga.Url))
        {
            try
            {
                Process.Start(new ProcessStartInfo(vaga.Url) { UseShellExecute = true });
                TxtStatusRodape.Text = $"Abrindo no navegador: {vaga.Titulo} ({vaga.Empresa})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o link: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnFavoritar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Vaga vaga)
        {
            vaga.Favorita = !vaga.Favorita;
            _databaseService.AlternarFavorita(vaga.Id, vaga.Favorita);

            // Atualiza a lista de vagas para refletir o binding
            ItemsVagas.Items.Refresh();
            CarregarVagasFavoritas();

            TxtStatusRodape.Text = vaga.Favorita
                ? $"Vaga #{vaga.Id} favoritada com sucesso!"
                : $"Vaga #{vaga.Id} removida dos favoritos.";
        }
    }

    private void BtnRemoverFavorita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Vaga vaga)
        {
            vaga.Favorita = false;
            _databaseService.AlternarFavorita(vaga.Id, false);

            CarregarVagasFavoritas();
            ItemsVagas.Items.Refresh();
            TxtStatusRodape.Text = $"Vaga #{vaga.Id} removida dos favoritos.";
        }
    }

    private void BtnRecarregarFavoritas_Click(object sender, RoutedEventArgs e)
    {
        CarregarVagasFavoritas();
        TxtStatusRodape.Text = "Lista de vagas salvas atualizada.";
    }

    private void BtnExportar_Click(object sender, RoutedEventArgs e)
    {
        if (_vagasAtuais.Count == 0)
        {
            MessageBox.Show("Nenhuma vaga encontrada na lista para exportar. Realize uma busca primeiro.", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "Arquivo CSV (*.csv)|*.csv",
            FileName = $"Vagas_WorkIT_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            Title = "Exportar Oportunidades para CSV"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                ExportService.ExportarParaCsv(saveDialog.FileName, _vagasAtuais);
                MessageBox.Show($"Arquivo exportado com sucesso!\n\nSalvo em: {saveDialog.FileName}", "Exportação Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusRodape.Text = $"Arquivo CSV gerado: {Path.GetFileName(saveDialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar arquivo: {ex.Message}", "Erro de Exportação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}