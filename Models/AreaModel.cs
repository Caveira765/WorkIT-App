using System.Collections.Generic;

namespace WorkIT.App.Models;

public class AreaModel
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public List<string> PalavrasChave { get; set; } = new();

    public override string ToString() => Nome;
}

public class EstadoModel
{
    public string Sigla { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;

    public string Display => string.IsNullOrWhiteSpace(Sigla) ? Nome : $"{Sigla} - {Nome}";

    public override string ToString() => Display;
}
