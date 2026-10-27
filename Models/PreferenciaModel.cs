namespace WorkIT.App.Models;

/// <summary>
/// Armazena as preferências de perfil do usuário persistidas no banco SQLite.
/// </summary>
public class PreferenciaUsuario
{
    public string NomeUsuario { get; set; } = string.Empty;
    public string AreaId { get; set; } = "0";
    public string NivelExperiencia { get; set; } = "Todos os Níveis";
    public string EstadoSigla { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public bool ApenasRemoto { get; set; }
    public bool TemaClaro { get; set; }
}

/// <summary>
/// Item de estatística para exibição de rankings (Cidades, Empresas e Áreas) com barra de progresso.
/// </summary>
public class EstatisticaRankingItem
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public double Percentual { get; set; }

    public string ResumoQuantidade => $"{Quantidade} vaga(s) • {Percentual:0.#}%";
}
