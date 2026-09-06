using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Inkboard.Infrastructure.Abstractions.Focus;

namespace Inkboard.Platform.Windows.Focus;

/// <summary>
/// Win32 前台进程名：供忽略规则匹配（用户填写 notepad / chrome 等 ProcessName）。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsForegroundAppInfo : IForegroundAppInfo
{
    public string? TryGetProcessName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;

            _ = GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
                return null;

            using var process = Process.GetProcessById((int)pid);
            return string.IsNullOrWhiteSpace(process.ProcessName) ? null : process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
