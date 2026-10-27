using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WorkIT.App.Data;
using WorkIT.App.Models;
using WorkIT.App.Services;

namespace WorkIT.App;

public partial class MainWindow : Window
{
    private readonly GupyApiService _apiService = new();
    private readonly DatabaseService _databaseService = new();

    // Lista completa da busca atual e lista filtrada exibida na tela
    private List<Vaga> _todasVagasCarregadas = new();
    private List<Vaga> _vagasAtuais = new();

    private int _paginaAtual = 1;
    private List<string> _ultimosTermosBusca = new();
    private string? _ultimoFiltroEstado;
    private string? _ultimoFiltroCidade;
    private string _ultimaAreaNome = "Geral";
    private bool _temaClaroAtivo;
    private bool _inicializando;

    public MainWindow()
    {
        _inicializando = true;
        InitializeComponent();

        InicializarCombos();
        CarregarPreferenciasUsuario();
        AtualizarEstatisticasEBanco();
        CarregarVagasFavoritas();
        _inicializando = false;
    }

    private void InicializarCombos()
    {
        // Combos da barra de filtros
        CmbArea.ItemsSource = VagaClassifierService.Areas;
        CmbArea.SelectedIndex = 0;

        CmbNivel.ItemsSource = VagaClassifierService.Niveis;
        CmbNivel.SelectedIndex = 0;

        CmbEstado.ItemsSource = VagaClassifierService.Estados;
        CmbEstado.SelectedIndex = 0;

        // Combos da aba de Preferências
        CmbPrefArea.ItemsSource = VagaClassifierService.Areas;
        CmbPrefArea.SelectedIndex = 0;

        CmbPrefNivel.ItemsSource = VagaClassifierService.Niveis;
        CmbPrefNivel.SelectedIndex = 0;

        CmbPrefEstado.ItemsSource = VagaClassifierService.Estados;
        CmbPrefEstado.SelectedIndex = 0;
    }

    // =========================================================
    // GERENCIAMENTO DE TEMA (ESCURO / CLARO)
    // =========================================================
    private void AplicarTema(bool temaClaro)
    {
        _temaClaroAtivo = temaClaro;

        void DefinirCor(string chave, string hexEscuro, string hexClaro)
        {
            string hex = temaClaro ? hexClaro : hexEscuro;
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Resources[chave] = new SolidColorBrush(color);
        }

        DefinirCor("BgWindow", "#0d0e17", "#f1f5f9");
        DefinirCor("BgHeader", "#12131f", "#ffffff");
        DefinirCor("BgFilterBar", "#141524", "#f8fafc");
        DefinirCor("BgCard", "#161828", "#ffffff");
        DefinirCor("BgSubCard", "#191d30", "#f1f5f9");
        DefinirCor("BgInput", "#191b2c", "#ffffff");
        DefinirCor("BgInputHover", "#202338", "#f1f5f9");
        DefinirCor("BgPopup", "#141524", "#ffffff");
        DefinirCor("BgSecondaryBtn", "#191b2c", "#ffffff");
        DefinirCor("BgSecondaryBtnHover", "#25283f", "#e2e8f0");
        DefinirCor("BgCardActionBtn", "#252a42", "#eff6ff");
        DefinirCor("FgCardActionBtn", "#f8fafc", "#1d4ed8");

        DefinirCor("BorderPrimary", "#1f2235", "#e2e8f0");
        DefinirCor("BorderCard", "#25293d", "#cbd5e1");
        DefinirCor("BorderInput", "#2e334d", "#cbd5e1");

        DefinirCor("TextPrimary", "#f8fafc", "#0f172a");
        DefinirCor("TextSecondary", "#94a3b8", "#475569");
        DefinirCor("TextMuted", "#64748b", "#64748b");
        DefinirCor("TextHighlight", "#93c5fd", "#2563eb");

        DefinirCor("ChipEncontradasBg", "#162035", "#eff6ff");
        DefinirCor("ChipNovasBg", "#122c22", "#ecfdf5");
        DefinirCor("ChipJaVistasBg", "#1c202d", "#e2e8f0");
        DefinirCor("ChipBancoBg", "#241738", "#f5f3ff");

        TxtBotaoTema.Text = temaClaro ? "🌙 Tema Escuro" : "☀️ Tema Claro";
        ChkPrefTemaClaro.IsChecked = temaClaro;
    }

    private void BtnAlternarTema_Click(object sender, RoutedEventArgs e)
    {
        AplicarTema(!_temaClaroAtivo);

        var pref = _databaseService.ObterPreferencias();
        pref.TemaClaro = _temaClaroAtivo;
        _databaseService.SalvarPreferencias(pref);

        TxtStatusRodape.Text = _temaClaroAtivo ? "Tema Claro ativado." : "Tema Escuro ativado.";
    }

    // =========================================================
    // NAVEGAÇÃO DA TELA INICIAL ("O QUE FAZEMOS")
    // =========================================================
    private void BtnAbrirInicio_Click(object sender, RoutedEventArgs e)
    {
        TelaInicialOverlay.Visibility = Visibility.Visible;
    }

    private void BtnComecarExplorar_Click(object sender, RoutedEventArgs e)
    {
        TelaInicialOverlay.Visibility = Visibility.Collapsed;
        MainTabs.SelectedIndex = 0;
    }

    private void BtnComecarPreferencias_Click(object sender, RoutedEventArgs e)
    {
        TelaInicialOverlay.Visibility = Visibility.Collapsed;
        MainTabs.SelectedIndex = 3;
    }

    // =========================================================
    // CADASTRO E APLICAÇÃO DE PREFERÊNCIAS DO USUÁRIO
    // =========================================================
    private void CarregarPreferenciasUsuario()
    {
        var pref = _databaseService.ObterPreferencias();

        // Preenche aba de preferências
        TxtPrefNome.Text = pref.NomeUsuario;
        TxtPrefCidade.Text = pref.Cidade;
        ChkPrefRemoto.IsChecked = pref.ApenasRemoto;
        ChkPrefTemaClaro.IsChecked = pref.TemaClaro;

        var areaObj = VagaClassifierService.Areas.FirstOrDefault(a => a.Id == pref.AreaId) ?? VagaClassifierService.Areas[0];
        CmbPrefArea.SelectedItem = areaObj;

        string nivelValido = VagaClassifierService.Niveis.Contains(pref.NivelExperiencia)
            ? pref.NivelExperiencia
            : "Todos os Níveis";
        CmbPrefNivel.SelectedItem = nivelValido;

        var estadoObj = VagaClassifierService.Estados.FirstOrDefault(e => e.Sigla == pref.EstadoSigla) ?? VagaClassifierService.Estados[0];
        CmbPrefEstado.SelectedItem = estadoObj;

        // Aplica preferências aos filtros principais e ao tema
        CmbArea.SelectedItem = areaObj;
        CmbNivel.SelectedItem = nivelValido;
        CmbEstado.SelectedItem = estadoObj;
        TxtCidade.Text = pref.Cidade;
        ChkApenasRemoto.IsChecked = pref.ApenasRemoto;

        AplicarTema(pref.TemaClaro);
        AtualizarBadgePerfil(pref);
    }

    private void AtualizarBadgePerfil(PreferenciaUsuario pref)
    {
        string saudacao = string.IsNullOrWhiteSpace(pref.NomeUsuario)
            ? "Perfil"
            : $"Olá, {pref.NomeUsuario.Trim()}";

        TxtSaudacaoPerfil.Text = $"{saudacao} • Nível: {pref.NivelExperiencia}";
    }

    private void BtnSalvarPreferencias_Click(object sender, RoutedEventArgs e)
    {
        var areaSel = CmbPrefArea.SelectedItem as AreaModel ?? VagaClassifierService.Areas[0];
        var estadoSel = CmbPrefEstado.SelectedItem as EstadoModel ?? VagaClassifierService.Estados[0];
        string nivelSel = CmbPrefNivel.SelectedItem as string ?? "Todos os Níveis";

        var pref = new PreferenciaUsuario
        {
            NomeUsuario = TxtPrefNome.Text.Trim(),
            AreaId = areaSel.Id,
            NivelExperiencia = nivelSel,
            EstadoSigla = estadoSel.Sigla,
            Cidade = TxtPrefCidade.Text.Trim(),
            ApenasRemoto = ChkPrefRemoto.IsChecked == true,
            TemaClaro = ChkPrefTemaClaro.IsChecked == true
        };

        _databaseService.SalvarPreferencias(pref);

        _inicializando = true;
        CmbArea.SelectedItem = areaSel;
        CmbNivel.SelectedItem = nivelSel;
        CmbEstado.SelectedItem = estadoSel;
        TxtCidade.Text = pref.Cidade;
        ChkApenasRemoto.IsChecked = pref.ApenasRemoto;
        _inicializando = false;

        AplicarTema(pref.TemaClaro);
        AtualizarBadgePerfil(pref);
        AplicarFiltrosLocais();

        MessageBox.Show(
            $"Suas preferências foram salvas com sucesso!\n\n• Área: {areaSel.Nome}\n• Nível: {nivelSel}\n• Local: {estadoSel.Display}",
            "Preferências Salvas",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );

        MainTabs.SelectedIndex = 0;
        TxtStatusRodape.Text = "Preferências de perfil salvas e aplicadas aos filtros.";
    }

    private void BtnRestaurarPreferencias_Click(object sender, RoutedEventArgs e)
    {
        var padrao = new PreferenciaUsuario();
        _databaseService.SalvarPreferencias(padrao);
        CarregarPreferenciasUsuario();
        AplicarFiltrosLocais();
        TxtStatusRodape.Text = "Preferências restauradas para o padrão.";
    }

    // =========================================================
    // ESTATÍSTICAS E BANCO DE DADOS LOCAL
    // =========================================================
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
            TxtQtdGeral.Text = stats.TryGetValue("Geral", out int gr) ? gr.ToString() : "0";

            var rankingCidades = _databaseService.ObterRankingCidades(8);
            ItemsRankingCidades.ItemsSource = rankingCidades;
            TxtEmptyCidades.Visibility = rankingCidades.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var rankingEmpresas = _databaseService.ObterRankingEmpresas(8);
            ItemsRankingEmpresas.ItemsSource = rankingEmpresas;
            TxtEmptyEmpresas.Visibility = rankingEmpresas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao atualizar estatísticas: {ex.Message}");
        }
    }

    private void BtnAtualizarEstatisticas_Click(object sender, RoutedEventArgs e)
    {
        AtualizarEstatisticasEBanco();
        TxtStatusRodape.Text = "Estatísticas de cidades, empresas e níveis atualizadas.";
    }

    private void BtnLimparHistorico_Click(object sender, RoutedEventArgs e)
    {
        var resp = MessageBox.Show(
            "Deseja remover do banco local as vagas do histórico que NÃO estão favoritadas?\n(Suas vagas salvas em '⭐ Vagas Salvas' serão mantidas).",
            "Limpar Histórico",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (resp == MessageBoxResult.Yes)
        {
            int removidas = _databaseService.LimparHistoricoNaoFavoritas();
            AtualizarEstatisticasEBanco();
            TxtStatusRodape.Text = $"{removidas} vagas não-favoritas removidas do histórico local.";
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

    private void BtnCarregarBanco_Click(object sender, RoutedEventArgs e)
    {
        _todasVagasCarregadas = _databaseService.ObterTodasSalvas();
        PanelCarregarMais.Visibility = Visibility.Collapsed;
        BtnCarregarMaisTopo.IsEnabled = false;

        MainTabs.SelectedIndex = 0;
        AplicarFiltrosLocais(origemBancoLocal: true);
    }

    // =========================================================
    // BUSCA E FILTRAGEM POR NÍVEL DE EXPERIÊNCIA
    // =========================================================
    private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && BtnBuscar.IsEnabled)
        {
            BtnBuscar_Click(sender, e);
        }
    }

    private void FiltroLocal_Changed(object sender, RoutedEventArgs e)
    {
        if (_inicializando || _todasVagasCarregadas.Count == 0) return;
        AplicarFiltrosLocais();
    }

    private void AplicarFiltrosLocais(bool origemBancoLocal = false)
    {
        string nivelSelecionado = CmbNivel.SelectedItem as string ?? "Todos os Níveis";
        bool apenasRemoto = ChkApenasRemoto.IsChecked == true;

        IEnumerable<Vaga> query = _todasVagasCarregadas;

        if (nivelSelecionado != "Todos os Níveis")
        {
            query = query.Where(v => string.Equals(v.Nivel, nivelSelecionado, StringComparison.OrdinalIgnoreCase));
        }

        if (apenasRemoto)
        {
            query = query.Where(v => v.EhRemota);
        }

        _vagasAtuais = query.ToList();

        ItemsVagas.ItemsSource = null;
        ItemsVagas.ItemsSource = _vagasAtuais;
        EmptyStatePlaceholder.Visibility = _vagasAtuais.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

        int novas = _vagasAtuais.Count(v => v.EhNova);
        int jaVistas = _vagasAtuais.Count - novas;

        TxtTotalEncontradas.Text = _vagasAtuais.Count.ToString();
        TxtTotalNovas.Text = novas.ToString();
        TxtTotalJaVistas.Text = jaVistas.ToString();

        string infoFiltro = nivelSelecionado != "Todos os Níveis" ? $" [Filtro Nível: {nivelSelecionado}]" : "";
        if (apenasRemoto) infoFiltro += " [Só Remoto]";

        if (_vagasAtuais.Count == 0)
        {
            TxtStatusLista.Text = $"Nenhuma vaga encontrada para os critérios atuais{infoFiltro}. Tente selecionar 'Todos os Níveis' ou carregar mais páginas.";
            TxtStatusRodape.Text = "0 vagas correspondem ao filtro selecionado.";
        }
        else
        {
            string fonte = origemBancoLocal ? "carregadas do banco SQLite" : $"encontradas (de {_todasVagasCarregadas.Count} coletadas)";
            TxtStatusLista.Text = $"Exibindo {_vagasAtuais.Count} oportunidades {fonte}{infoFiltro}.";
            TxtStatusRodape.Text = $"Lista atualizada: {_vagasAtuais.Count} vagas exibidas ({novas} novas, {jaVistas} já vistas).";
        }
    }

    private List<string> MontarTermosDeBusca(AreaModel? areaSelecionada, string termoExtra, string nivelSelecionado)
    {
        var termos = new List<string>();
        string? termoNivel = VagaClassifierService.ObterTermoBuscaNivel(nivelSelecionado);

        if (!string.IsNullOrWhiteSpace(termoExtra))
        {
            termos.Add(termoExtra);
            if (termoNivel != null) termos.Add($"{termoExtra} {termoNivel}");
        }

        if (areaSelecionada != null)
        {
            if (areaSelecionada.Id == "0")
            {
                if (termos.Count == 0)
                {
                    if (termoNivel != null)
                    {
                        termos.AddRange(new[] { $"desenvolvedor {termoNivel}", $"ti {termoNivel}", "desenvolvedor", "software" });
                    }
                    else
                    {
                        termos.AddRange(new[] { "desenvolvedor", "programador", "software", "tecnologia" });
                    }
                }
            }
            else
            {
                var chavesPrincipais = areaSelecionada.PalavrasChave.Take(3).ToList();
                if (termoNivel != null && chavesPrincipais.Count > 0)
                {
                    termos.Add($"{chavesPrincipais[0]} {termoNivel}");
                }
                termos.AddRange(chavesPrincipais);
            }
        }

        if (termos.Count == 0)
        {
            termos.Add("desenvolvedor");
        }

        return termos.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        AlternarEstadoBotoesBusca(ocupado: true);
        PanelCarregarMais.Visibility = Visibility.Collapsed;
        TxtStatusRodape.Text = "Consultando oportunidades na API da Gupy...";
        MainTabs.SelectedIndex = 0;

        var areaSelecionada = CmbArea.SelectedItem as AreaModel;
        var estadoSelecionado = CmbEstado.SelectedItem as EstadoModel;
        string nivelSelecionado = CmbNivel.SelectedItem as string ?? "Todos os Níveis";
        string cidade = TxtCidade.Text.Trim();
        string termoExtra = TxtTermoExtra.Text.Trim();

        string? filtroEstado = string.IsNullOrWhiteSpace(estadoSelecionado?.Nome) || string.IsNullOrEmpty(estadoSelecionado.Sigla)
            ? null
            : estadoSelecionado.Nome;

        string? filtroCidade = string.IsNullOrWhiteSpace(cidade) ? null : cidade;

        _ultimosTermosBusca = MontarTermosDeBusca(areaSelecionada, termoExtra, nivelSelecionado);
        _ultimoFiltroEstado = filtroEstado;
        _ultimoFiltroCidade = filtroCidade;
        _ultimaAreaNome = areaSelecionada?.Nome ?? "Geral";
        _paginaAtual = 2;

        try
        {
            var vagasMapeadas = new Dictionary<long, Vaga>();

            await Task.Run(async () =>
            {
                foreach (var termo in _ultimosTermosBusca)
                {
                    var resultados = await _apiService.BuscarVagasPorPalavraAsync(
                        termo,
                        filtroEstado,
                        filtroCidade,
                        paginaInicial: 1,
                        quantidadePaginas: 2
                    );

                    foreach (var v in resultados)
                    {
                        if (!vagasMapeadas.ContainsKey(v.Id))
                        {
                            v.Area = _ultimaAreaNome;
                            vagasMapeadas[v.Id] = v;
                        }
                    }
                }
            });

            _todasVagasCarregadas = vagasMapeadas.Values.ToList();

            foreach (var vaga in _todasVagasCarregadas)
            {
                _databaseService.SalvarVaga(vaga);
            }

            AplicarFiltrosLocais();
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
            AlternarEstadoBotoesBusca(ocupado: false);
        }
    }

    private async void BtnCarregarMais_Click(object sender, RoutedEventArgs e)
    {
        if (_ultimosTermosBusca.Count == 0)
        {
            BtnBuscar_Click(sender, e);
            return;
        }

        AlternarEstadoBotoesBusca(ocupado: true);

        int proximaPagina = _paginaAtual + 1;
        const int qtdPaginas = 2;
        TxtStatusRodape.Text = $"Buscando mais vagas (páginas {proximaPagina} e {proximaPagina + qtdPaginas - 1})...";

        try
        {
            var novasVagasColetadas = new List<Vaga>();
            var idsExistentes = new HashSet<long>(_todasVagasCarregadas.Select(v => v.Id));

            await Task.Run(async () =>
            {
                foreach (var termo in _ultimosTermosBusca)
                {
                    var resultados = await _apiService.BuscarVagasPorPalavraAsync(
                        termo,
                        _ultimoFiltroEstado,
                        _ultimoFiltroCidade,
                        paginaInicial: proximaPagina,
                        quantidadePaginas: qtdPaginas
                    );

                    foreach (var v in resultados)
                    {
                        if (idsExistentes.Add(v.Id))
                        {
                            v.Area = _ultimaAreaNome;
                            novasVagasColetadas.Add(v);
                        }
                    }
                }
            });

            _paginaAtual += qtdPaginas;

            if (novasVagasColetadas.Count > 0)
            {
                foreach (var vaga in novasVagasColetadas)
                {
                    _databaseService.SalvarVaga(vaga);
                }

                _todasVagasCarregadas.AddRange(novasVagasColetadas);
                AplicarFiltrosLocais();
                AtualizarEstatisticasEBanco();
                CarregarVagasFavoritas();

                TxtStatusRodape.Text = $"+{novasVagasColetadas.Count} novas vagas carregadas da API (Total na sessão: {_todasVagasCarregadas.Count}).";
            }
            else
            {
                TxtStatusRodape.Text = $"Não foram encontradas vagas inéditas nas páginas {proximaPagina}-{proximaPagina + qtdPaginas - 1}.";
                MessageBox.Show(
                    "Não foram encontradas vagas adicionais para os filtros selecionados nas próximas páginas.",
                    "Fim dos Resultados",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocorreu um erro ao carregar mais vagas: {ex.Message}", "Erro de Comunicação", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatusRodape.Text = "Erro durante o carregamento de mais vagas.";
        }
        finally
        {
            AlternarEstadoBotoesBusca(ocupado: false);
        }
    }

    private void AlternarEstadoBotoesBusca(bool ocupado)
    {
        BtnBuscar.IsEnabled = !ocupado;
        BtnExportar.IsEnabled = !ocupado;
        BtnCarregarBanco.IsEnabled = !ocupado;
        BtnCarregarMaisTopo.IsEnabled = !ocupado && _todasVagasCarregadas.Count > 0;
        BtnCarregarMaisFim.IsEnabled = !ocupado;
        PanelCarregarMais.Visibility = (!ocupado && _todasVagasCarregadas.Count > 0 && _ultimosTermosBusca.Count > 0)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProgressBusca.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
    }

    // =========================================================
    // AÇÕES DOS CARDS E EXPORTAÇÃO CSV
    // =========================================================
    private void BtnAbrirLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Vaga vaga } && !string.IsNullOrWhiteSpace(vaga.Url))
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
        if (sender is Button { Tag: Vaga vaga })
        {
            vaga.Favorita = !vaga.Favorita;
            _databaseService.AlternarFavorita(vaga.Id, vaga.Favorita);

            ItemsVagas.Items.Refresh();
            CarregarVagasFavoritas();

            TxtStatusRodape.Text = vaga.Favorita
                ? $"Vaga #{vaga.Id} salva nos favoritos!"
                : $"Vaga #{vaga.Id} removida dos favoritos.";
        }
    }

    private void BtnRemoverFavorita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Vaga vaga })
        {
            vaga.Favorita = false;
            _databaseService.AlternarFavorita(vaga.Id, false);

            var itemNaLista = _todasVagasCarregadas.FirstOrDefault(v => v.Id == vaga.Id);
            if (itemNaLista != null) itemNaLista.Favorita = false;

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
            MessageBox.Show("Nenhuma vaga na lista atual para exportar. Realize uma busca ou carregue o banco primeiro.", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
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
