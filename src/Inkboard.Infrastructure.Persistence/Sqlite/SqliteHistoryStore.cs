namespace Inkboard.Infrastructure.Persistence.Sqlite;

using Inkboard.Domain.Entities;
using Inkboard.Domain.Enums;
using Inkboard.Infrastructure.Abstractions.Persistence;
using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite 历史库：重启仍在；文本/图片共用一张表，payload 存 BLOB。
/// </summary>
public sealed class SqliteHistoryStore : IHistoryStore, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteHistoryStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Inkboard",
            "history.db"))
    {
    }

    /// <summary>测试可注入临时路径。</summary>
    public SqliteHistoryStore(string databasePath)
    {
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    public async Task<IReadOnlyList<HistoryItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = Open();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT id, preview, kind, source_app, copied_at, pin_key, payload
                FROM history;
                """;

            var list = new List<HistoryItem>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                list.Add(ReadItem(reader));
            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(HistoryItem item, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = Open();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO history (id, preview, kind, source_app, copied_at, pin_key, payload)
                VALUES ($id, $preview, $kind, $source, $copied, $pin, $payload)
                ON CONFLICT(id) DO UPDATE SET
                  preview = excluded.preview,
                  kind = excluded.kind,
                  source_app = excluded.source_app,
                  copied_at = excluded.copied_at,
                  pin_key = excluded.pin_key,
                  payload = excluded.payload;
                """;
            Bind(cmd, item);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = Open();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM history WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id.ToString("N"));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearUnpinnedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = Open();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM history WHERE pin_key IS NULL;";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await using var connection = Open();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS history (
              id TEXT PRIMARY KEY NOT NULL,
              preview TEXT NOT NULL,
              kind INTEGER NOT NULL,
              source_app TEXT NULL,
              copied_at TEXT NOT NULL,
              pin_key TEXT NULL,
              payload BLOB NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_history_copied_at ON history(copied_at);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    private static void Bind(SqliteCommand cmd, HistoryItem item)
    {
        cmd.Parameters.AddWithValue("$id", item.Id.ToString("N"));
        cmd.Parameters.AddWithValue("$preview", item.Preview);
        cmd.Parameters.AddWithValue("$kind", (int)item.Kind);
        cmd.Parameters.AddWithValue("$source", (object?)item.SourceApp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$copied", item.CopiedAt.UtcDateTime.ToString("O"));
        cmd.Parameters.AddWithValue("$pin", (object?)item.PinKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$payload", item.Payload);
    }

    private static HistoryItem ReadItem(SqliteDataReader reader)
    {
        var payload = reader.IsDBNull(6)
            ? Array.Empty<byte>()
            : reader.GetFieldValue<byte[]>(6);

        return new HistoryItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            Preview = reader.GetString(1),
            Kind = (ClipboardContentKind)reader.GetInt32(2),
            SourceApp = reader.IsDBNull(3) ? null : reader.GetString(3),
            CopiedAt = DateTimeOffset.Parse(
                reader.GetString(4),
                null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            PinKey = reader.IsDBNull(5) ? null : reader.GetString(5),
            Payload = payload,
        };
    }
}
