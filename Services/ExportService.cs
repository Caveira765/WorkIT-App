using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WorkIT.App.Models;

namespace WorkIT.App.Services;

public class ExportService
{
    public static void ExportarParaCsv(string caminhoArquivo, IEnumerable<Vaga> vagas)
    {
        var sb = new StringBuilder();

        // Cabeçalho compatível com Excel em português (separador ponto e vírgula)
        sb.AppendLine("ID;Título;Empresa;Cidade;Estado;Nível;Área;Status;Favorita;Data Publicação;Link");

        foreach (var v in vagas)
        {
            string id = v.Id.ToString();
            string titulo = EscaparCampoCsv(v.Titulo);
            string empresa = EscaparCampoCsv(v.Empresa);
            string cidade = EscaparCampoCsv(v.Cidade);
            string estado = EscaparCampoCsv(v.Estado);
            string nivel = EscaparCampoCsv(v.Nivel);
            string area = EscaparCampoCsv(v.Area);
            string status = v.EhNova ? "Nova" : "Já Vista";
            string favorita = v.Favorita ? "Sim" : "Não";
            string dataPub = EscaparCampoCsv(v.DataPublicacao);
            string link = EscaparCampoCsv(v.Url);

            sb.AppendLine($"{id};{titulo};{empresa};{cidade};{estado};{nivel};{area};{status};{favorita};{dataPub};{link}");
        }

        // Escreve com BOM UTF-8 para que o Excel abra acentos perfeitamente sem corromper
        File.WriteAllText(caminhoArquivo, sb.ToString(), new UTF8Encoding(true));
    }

    private static string EscaparCampoCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        // Se contiver ponto e vírgula, quebra de linha ou aspas, precisa ser delimitado por aspas
        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
        {
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }

        return valor;
    }
}
