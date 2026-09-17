using System;
using System.Collections.Generic;
using System.IO;
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
        command.Parameters.AddWithValue("$nivel", vaga.Nivel ?? "Não identificado");
        command.Parameters.AddWithValue("$area", vaga.Area ?? string.Empty);
        command.Parameters.AddWithValue("$data_publicacao", vaga.DataPublicacao ?? string.Empty);
        command.Parameters.AddWithValue("$data_salvo", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$favorita", vaga.Favorita ? 1 : 0);

        int rows = command.ExecuteNonQuery();
        bool ehNova = rows > 0;
        vaga.EhNova = ehNova;

        if (!ehNova)
        {
            // Se já existia, consulta se ela já foi favoritada
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

    public List<Vaga> ObterFavoritas()
    {
        return ConsultarVagas("SELECT * FROM vagas WHERE favorita = 1 ORDER BY id DESC");
    }

    public List<Vaga> ObterTodasSalvas()
    {
        return ConsultarVagas("SELECT * FROM vagas ORDER BY id DESC");
    }

    public int ObterTotalNoBanco()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM vagas;";
        return Convert.ToInt32(command.ExecuteScalar());
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
            string nivel = reader.GetString(0);
            int qtd = reader.GetInt32(1);
            dic[nivel] = qtd;
        }

        return dic;
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
                Nivel = reader.IsDBNull(7) ? "Não identificado" : reader.GetString(7),
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
