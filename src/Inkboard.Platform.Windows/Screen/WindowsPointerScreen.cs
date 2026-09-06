using System.Runtime.InteropServices;
using Inkboard.Infrastructure.Abstractions.Screen;

namespace Inkboard.Platform.Windows.Screen;

/// <summary>
/// Win32 光标与显示器工作区，供弹出层贴光标定位。
/// </summary>
public sealed class WindowsPointerScreen : IPointerScreen
{
    public ScreenPoint GetCursorPosition()
    {
        if (!GetCursorPos(out var pt))
            return new ScreenPoint(0, 0);
        return new ScreenPoint(pt.X, pt.Y);
    }

    public ScreenRect GetWorkingAreaContaining(ScreenPoint point)
    {
        var monitor = MonitorFromPoint(new POINT { X = point.X, Y = point.Y }, MonitorDefaultToNearest);
        var info = new MONITORINFO { CbSize = Marshal.SizeOf<MONITORINFO>() };
        if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
        {
            var r = info.RcWork;
            return new ScreenRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

        return new ScreenRect(0, 0, 1920, 1080);
    }

    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int CbSize;
        public RECT RcMonitor;
        public RECT RcWork;
        public uint DwFlags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}
