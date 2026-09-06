using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Inkboard.Infrastructure.Abstractions.Focus;

namespace Inkboard.Platform.Windows.Focus;

/// <summary>
/// Win32 前台窗口记忆：热键弹出前记下 HWND，关闭后再 SetForegroundWindow。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsForegroundFocus : IForegroundFocus
{
    private IntPtr _hwnd;

    public void Capture()
    {
        _hwnd = GetForegroundWindow();
    }

    public bool TryRestore()
    {
        if (_hwnd == IntPtr.Zero)
            return false;

        return SetForegroundWindow(_hwnd);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
