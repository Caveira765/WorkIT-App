using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WorkIT.App.Models;

namespace WorkIT.App.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        string dbPath = Path.Combine(AppContext.BaseDirectory, "vagas.db");
        _connectionString = $"Data Source={dbPath}";
        InicializarBanco();
    }

    private void InicializarBanco()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS vagas (
                id INTEGER PRIMARY KEY,
                titulo TEXT,
                empresa TEXT,
                cidade TEXT,
                estado TEXT,
                url TEXT,
                tipo TEXT,
                nivel TEXT,
                area TEXT,
                data_publicacao TEXT,
                data_salvo TEXT,
                favorita INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS preferencias (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                nome_usuario TEXT,
                area_id TEXT,
                nivel_experiencia TEXT,
                estado_sigla TEXT,
                cidade TEXT,
                apenas_remoto INTEGER DEFAULT 0,
                tema_claro INTEGER DEFAULT 0
            );
        ";
        command.ExecuteNonQuery();
    }

    public bool SalvarVaga(Vaga vaga)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO vagas (
                id, titulo, empresa, cidade, estado,
                url, tipo, nivel, area, data_publicacao, data_salvo, favorita
            ) VALUES (
                $id, $titulo, $empresa, $cidade, $estado,
                $url, $tipo, $nivel, $area, $data_publicacao, $data_salvo, $favorita
            );
        ";

        command.Parameters.AddWithValue("$id", vaga.Id);
        command.Parameters.AddWithValue("$titulo", vaga.Titulo ?? string.Empty);
        command.Parameters.AddWithValue("$empresa", vaga.Empresa ?? string.Empty);
        command.Parameters.AddWithValue("$cidade", vaga.Cidade ?? string.Empty);
        command.Parameters.AddWithValue("$estado", vaga.Estado ?? string.Empty);
        command.Parameters.AddWithValue("$url", vaga.Url ?? string.Empty);
        command.Parameters.AddWithValue("$tipo", vaga.Tipo ?? string.Empty);
        command.Parameters.AddWithValue("$nivel", vaga.Nivel ?? "Geral");
        command.Parameters.AddWithValue("$area", vaga.Area ?? string.Empty);
        command.Parameters.AddWithValue("$data_publicacao", vaga.DataPublicacao ?? string.Empty);
        command.Parameters.AddWithValue("$data_salvo", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$favorita", vaga.Favorita ? 1 : 0);

        int rows = command.ExecuteNonQuery();
        bool ehNova = rows > 0;
        vaga.EhNova = ehNova;

        if (!ehNova)
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT favorita FROM vagas WHERE id = $id;";
            checkCmd.Parameters.AddWithValue("$id", vaga.Id);
            var favResult = checkCmd.ExecuteScalar();
            if (favResult != null && favResult != DBNull.Value)
            {
                vaga.Favorita = Convert.ToInt32(favResult) == 1;
            }
        }

        return ehNova;
    }

    public void AlternarFavorita(long id, bool favorita)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE vagas SET favorita = $fav WHERE id = $id;";
        command.Parameters.AddWithValue("$fav", favorita ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public List<Vaga> ObterFavoritas() =>
        ConsultarVagas("SELECT * FROM vagas WHERE favorita = 1 ORDER BY id DESC");

    public List<Vaga> ObterTodasSalvas() =>
        ConsultarVagas("SELECT * FROM vagas ORDER BY id DESC");

    public int ObterTotalNoBanco()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM vagas;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int LimparHistoricoNaoFavoritas()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM vagas WHERE favorita = 0;";
        return command.ExecuteNonQuery();
    }

    public Dictionary<string, int> ObterEstatisticasNivel()
    {
        var dic = new Dictionary<string, int>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT nivel, COUNT(*) as qtd FROM vagas GROUP BY nivel ORDER BY qtd DESC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string nivel = reader.IsDBNull(0) ? "Geral" : reader.GetString(0);
            int qtd = reader.GetInt32(1);
            dic[nivel] = qtd;
        }

        return dic;
    }

    /// <summary>
    /// Agrupa as vagas por Cidade/Estado e mostra também quais empresas contratam em cada local.
    /// </summary>
    public List<EstatisticaRankingItem> ObterRankingCidades(int limite = 8)
    {
        var vagas = ObterTodasSalvas();
        if (vagas.Count == 0) return new List<EstatisticaRankingItem>();

        return vagas
            .GroupBy(v => string.IsNullOrWhiteSpace(v.Cidade) ? "Remoto / Brasil" : v.LocalFormatado)
            .OrderByDescending(g => g.Count())
            .Take(limite)
            .Select(g =>
            {
                var principaisEmpresas = g
                    .Select(v => v.Empresa)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .Take(3);

                return new EstatisticaRankingItem
                {
                    Titulo = g.Key,
                    Subtitulo = $"Empresas: {string.Join(", ", principaisEmpresas)}",
                    Quantidade = g.Count(),
                    Percentual = (double)g.Count() / vagas.Count * 100.0
                };
            })
            .ToList();
    }

    /// <summary>
    /// Agrupa as vagas por Empresa e mostra em quais cidades cada empresa está contratando.
    /// </summary>
    public List<EstatisticaRankingItem> ObterRankingEmpresas(int limite = 8)
    {
        var vagas = ObterTodasSalvas();
        if (vagas.Count == 0) return new List<EstatisticaRankingItem>();

        return vagas
            .GroupBy(v => string.IsNullOrWhiteSpace(v.Empresa) ? "Empresa Confidencial" : v.Empresa.Trim())
            .OrderByDescending(g => g.Count())
            .Take(limite)
            .Select(g =>
            {
                var principaisCidades = g
                    .Select(v => string.IsNullOrWhiteSpace(v.Cidade) ? "Remoto" : v.Cidade)
                    .Distinct()
                    .Take(3);

                return new EstatisticaRankingItem
                {
                    Titulo = g.Key,
                    Subtitulo = $"Locais: {string.Join(", ", principaisCidades)}",
                    Quantidade = g.Count(),
                    Percentual = (double)g.Count() / vagas.Count * 100.0
                };
            })
            .ToList();
    }

    public void SalvarPreferencias(PreferenciaUsuario pref)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO preferencias (
                id, nome_usuario, area_id, nivel_experiencia,
                estado_sigla, cidade, apenas_remoto, tema_claro
            ) VALUES (
                1, $nome, $area, $nivel, $estado, $cidade, $remoto, $tema
            );
        ";

        command.Parameters.AddWithValue("$nome", pref.NomeUsuario ?? string.Empty);
        command.Parameters.AddWithValue("$area", pref.AreaId ?? "0");
        command.Parameters.AddWithValue("$nivel", pref.NivelExperiencia ?? "Todos os Níveis");
        command.Parameters.AddWithValue("$estado", pref.EstadoSigla ?? string.Empty);
        command.Parameters.AddWithValue("$cidade", pref.Cidade ?? string.Empty);
        command.Parameters.AddWithValue("$remoto", pref.ApenasRemoto ? 1 : 0);
        command.Parameters.AddWithValue("$tema", pref.TemaClaro ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public PreferenciaUsuario ObterPreferencias()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT nome_usuario, area_id, nivel_experiencia, estado_sigla, cidade, apenas_remoto, tema_claro
            FROM preferencias WHERE id = 1;
        ";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new PreferenciaUsuario
            {
                NomeUsuario = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                AreaId = reader.IsDBNull(1) ? "0" : reader.GetString(1),
                NivelExperiencia = reader.IsDBNull(2) ? "Todos os Níveis" : reader.GetString(2),
                EstadoSigla = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Cidade = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ApenasRemoto = !reader.IsDBNull(5) && reader.GetInt32(5) == 1,
                TemaClaro = !reader.IsDBNull(6) && reader.GetInt32(6) == 1
            };
        }

        return new PreferenciaUsuario();
    }

    private List<Vaga> ConsultarVagas(string sql)
    {
        var lista = new List<Vaga>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new Vaga
            {
                Id = reader.GetInt64(0),
                Titulo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Empresa = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Cidade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Estado = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Url = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Tipo = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Nivel = reader.IsDBNull(7) ? "Geral" : reader.GetString(7),
                Area = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                DataPublicacao = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                DataSalvo = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                Favorita = !reader.IsDBNull(11) && reader.GetInt32(11) == 1,
                EhNova = false
            });
        }

        return lista;
    }
}
