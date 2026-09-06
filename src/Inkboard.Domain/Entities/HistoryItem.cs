namespace Inkboard.Domain.Entities;

using Inkboard.Domain.Enums;

/// <summary>
/// 一条剪贴板历史记录（领域实体，与存储/UI 无关）。
/// </summary>
public sealed class HistoryItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>用于列表展示与搜索的纯文本预览。</summary>
    public string Preview { get; set; } = string.Empty;

    public ClipboardContentKind Kind { get; init; } = ClipboardContentKind.Text;

    /// <summary>来源应用标识（如进程名 / bundle id），可为空。</summary>
    public string? SourceApp { get; set; }

    public DateTimeOffset CopiedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>置顶后排在列表前部；未置顶为 null。</summary>
    public string? PinKey { get; set; }

    public bool IsPinned => PinKey is not null;

    /// <summary>原始载荷（文本 UTF-8 或 PNG 等字节）。按 Kind 解释。</summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
