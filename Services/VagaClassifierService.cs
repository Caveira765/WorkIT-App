using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WorkIT.App.Models;

namespace WorkIT.App.Services;

public class VagaClassifierService
{
    public static readonly List<AreaModel> Areas = new()
    {
        new AreaModel
        {
            Id = "0",
            Nome = "Todas as Áreas de TI",
            PalavrasChave = new List<string> { "desenvolvedor", "software", "tecnologia", "programador", "ti" }
        },
        new AreaModel
        {
            Id = "1",
            Nome = "RPA / Automação",
            PalavrasChave = new List<string> { "rpa", "automação", "automacao", "uipath", "power automate", "robot framework" }
        },
        new AreaModel
        {
            Id = "2",
            Nome = "Dev Fullstack",
            PalavrasChave = new List<string> { "fullstack", "full stack", "full-stack" }
        },
        new AreaModel
        {
            Id = "3",
            Nome = "Dev Frontend",
            PalavrasChave = new List<string> { "frontend", "front-end", "react", "vue", "angular" }
        },
        new AreaModel
        {
            Id = "4",
            Nome = "Dev Backend",
            PalavrasChave = new List<string> { "backend", "back-end", "c#", ".net", "java", "python", "node" }
        },
        new AreaModel
        {
            Id = "5",
            Nome = "Cybersecurity",
            PalavrasChave = new List<string> { "cybersecurity", "segurança da informação", "pentest", "soc" }
        },
        new AreaModel
        {
            Id = "6",
            Nome = "Dados / BI / Analytics",
            PalavrasChave = new List<string> { "dados", "bi", "analytics", "power bi", "sql", "data engineer" }
        },
        new AreaModel
        {
            Id = "7",
            Nome = "DevOps / Cloud",
            PalavrasChave = new List<string> { "devops", "cloud", "aws", "azure", "kubernetes", "docker" }
        },
        new AreaModel
        {
            Id = "8",
            Nome = "Mobile",
            PalavrasChave = new List<string> { "mobile", "android", "ios", "flutter", "react native" }
        },
        new AreaModel
        {
            Id = "9",
            Nome = "QA / Testes",
            PalavrasChave = new List<string> { "qa", "quality assurance", "testes", "tester", "cypress" }
        },
        new AreaModel
        {
            Id = "10",
            Nome = "Redes / Infraestrutura",
            PalavrasChave = new List<string> { "infraestrutura", "redes", "suporte ti", "sysadmin" }
        },
        new AreaModel
        {
            Id = "11",
            Nome = "IA / Machine Learning",
            PalavrasChave = new List<string> { "machine learning", "inteligência artificial", "llm", "ia" }
        }
    };

    public static readonly List<EstadoModel> Estados = new()
    {
        new EstadoModel { Sigla = "", Nome = "Todos os Estados" },
        new EstadoModel { Sigla = "AC", Nome = "Acre" },
        new EstadoModel { Sigla = "AL", Nome = "Alagoas" },
        new EstadoModel { Sigla = "AP", Nome = "Amapá" },
        new EstadoModel { Sigla = "AM", Nome = "Amazonas" },
        new EstadoModel { Sigla = "BA", Nome = "Bahia" },
        new EstadoModel { Sigla = "CE", Nome = "Ceará" },
        new EstadoModel { Sigla = "DF", Nome = "Distrito Federal" },
        new EstadoModel { Sigla = "ES", Nome = "Espírito Santo" },
        new EstadoModel { Sigla = "GO", Nome = "Goiás" },
        new EstadoModel { Sigla = "MA", Nome = "Maranhão" },
        new EstadoModel { Sigla = "MT", Nome = "Mato Grosso" },
        new EstadoModel { Sigla = "MS", Nome = "Mato Grosso do Sul" },
        new EstadoModel { Sigla = "MG", Nome = "Minas Gerais" },
        new EstadoModel { Sigla = "PA", Nome = "Pará" },
        new EstadoModel { Sigla = "PB", Nome = "Paraíba" },
        new EstadoModel { Sigla = "PR", Nome = "Paraná" },
        new EstadoModel { Sigla = "PE", Nome = "Pernambuco" },
        new EstadoModel { Sigla = "PI", Nome = "Piauí" },
        new EstadoModel { Sigla = "RJ", Nome = "Rio de Janeiro" },
        new EstadoModel { Sigla = "RN", Nome = "Rio Grande do Norte" },
        new EstadoModel { Sigla = "RS", Nome = "Rio Grande do Sul" },
        new EstadoModel { Sigla = "RO", Nome = "Rondônia" },
        new EstadoModel { Sigla = "RR", Nome = "Roraima" },
        new EstadoModel { Sigla = "SC", Nome = "Santa Catarina" },
        new EstadoModel { Sigla = "SP", Nome = "São Paulo" },
        new EstadoModel { Sigla = "SE", Nome = "Sergipe" },
        new EstadoModel { Sigla = "TO", Nome = "Tocantins" }
    };

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var normalizedString = texto.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
    }

    public static string DetectarNivel(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo)) return "Não identificado";

        string normalizado = Normalizar(titulo);

        if (normalizado.Contains("estagio") || normalizado.Contains("estagiario") || normalizado.Contains("intern") || normalizado.Contains("trainee"))
            return "Estágio";

        if (normalizado.Contains("junior") || normalizado.Contains("jr"))
            return "Júnior";

        if (normalizado.Contains("senior") || normalizado.Contains("sr") || normalizado.Contains("especialista") || normalizado.Contains("lead") || normalizado.Contains("principal"))
            return "Sênior";

        if (normalizado.Contains("pleno") || normalizado.Contains("pl"))
            return "Pleno";

        return "Geral";
    }
}
