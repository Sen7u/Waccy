namespace Inkboard.Infrastructure.Abstractions.Screen;

/// <summary>
/// 指针与屏幕工作区查询（对齐 Maccy popupPosition=cursor）。
/// 坐标为屏幕像素，原点在左上，Y 向下。
/// </summary>
public interface IPointerScreen
{
    ScreenPoint GetCursorPosition();

    /// <summary>包含指定点的显示器工作区（已排除任务栏）。</summary>
    ScreenRect GetWorkingAreaContaining(ScreenPoint point);
}

public readonly record struct ScreenPoint(int X, int Y);

public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
