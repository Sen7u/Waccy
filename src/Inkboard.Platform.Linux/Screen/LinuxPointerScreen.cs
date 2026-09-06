using Inkboard.Infrastructure.Abstractions.Screen;

namespace Inkboard.Platform.Linux.Screen;

/// <summary>
/// Linux 骨架：尚无可靠全局光标 API 时，退回主屏工作区中心。
/// 真正定位由 PopupHost 再用 Avalonia Screens 钳制。
/// </summary>
public sealed class LinuxPointerScreen : IPointerScreen
{
    public ScreenPoint GetCursorPosition()
    {
        var area = GetWorkingAreaContaining(new ScreenPoint(0, 0));
        return new ScreenPoint(area.X + area.Width / 2, area.Y + area.Height / 2);
    }

    public ScreenRect GetWorkingAreaContaining(ScreenPoint point)
        => new(0, 0, 1920, 1080);
}
