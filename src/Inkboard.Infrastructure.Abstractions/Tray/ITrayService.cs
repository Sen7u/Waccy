namespace Inkboard.Infrastructure.Abstractions.Tray;

/// <summary>
/// 系统托盘交互端口。UI 层也可直接用 Avalonia TrayIcon；
/// 该接口留给需要与平台菜单深度集成时使用。
/// </summary>
public interface ITrayService
{
    void SetPaused(bool paused);
}
