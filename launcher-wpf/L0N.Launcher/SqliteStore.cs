using Microsoft.Data.Sqlite;
using System.IO;
using L0N.Core;

namespace L0N.Launcher;

public sealed class SqliteStore
{
    private readonly string _connectionString;
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "L0NLauncher");

    public SqliteStore()
    {
        Directory.CreateDirectory(DataDirectory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(DataDirectory, "launcher.db"),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS library (
                id TEXT PRIMARY KEY NOT NULL,
                title TEXT NOT NULL,
                source TEXT NOT NULL,
                page_url TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
                name TEXT PRIMARY KEY NOT NULL,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public IReadOnlyList<LibraryEntry> GetLibrary()
    {
        var result = new List<LibraryEntry>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id,title,source,page_url FROM library ORDER BY title COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new LibraryEntry(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        return result;
    }

    public void SaveGame(GameEntry item)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO library(id,title,source,page_url) VALUES ($id,$title,$source,$url) " +
                          "ON CONFLICT(id) DO UPDATE SET title=$title,source=$source,page_url=$url";
        cmd.Parameters.AddWithValue("$id", item.Id);
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$source", item.Source);
        cmd.Parameters.AddWithValue("$url", (object?)item.PageUrl ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void RemoveGame(string id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM library WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public string GetSetting(string name, string fallback)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE name=$name LIMIT 1";
        cmd.Parameters.AddWithValue("$name", name);
        return cmd.ExecuteScalar() as string ?? fallback;
    }

    public void SetSetting(string name, string value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO settings(name,value) VALUES ($name,$value) " +
                          "ON CONFLICT(name) DO UPDATE SET value=$value";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }
}

public sealed record LibraryEntry(string Id, string Title, string Source, string? PageUrl)
{
    public string SubTitle => Source;
}
