namespace Inkboard.Infrastructure.Persistence.Json;

using System.Text.Json;
using Inkboard.Infrastructure.Abstractions.Settings;

/// <summary>
/// 用户目录下的 JSON 设置文件。先写临时文件再替换，避免半写入损坏。
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _path;
    private readonly object _gate = new();

    public JsonSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Inkboard",
            "settings.json"))
    {
    }

    /// <summary>测试可注入自定义路径。</summary>
    public JsonSettingsStore(string path) => _path = path;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
                return Task.FromResult(new AppSettings());

            try
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return Task.FromResult(loaded?.Clone() ?? new AppSettings());
            }
            catch
            {
                // 损坏文件时回退默认，不阻断启动
                return Task.FromResult(new AppSettings());
            }
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings.Clone(), JsonOptions);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, json);
            File.Copy(temp, _path, overwrite: true);
            File.Delete(temp);
        }

        return Task.CompletedTask;
    }
}
