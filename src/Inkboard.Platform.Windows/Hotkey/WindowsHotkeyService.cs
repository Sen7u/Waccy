using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Inkboard.Infrastructure.Abstractions.Hotkey;

namespace Inkboard.Platform.Windows.Hotkey;

/// <summary>
/// Windows 全局热键：RegisterHotKey + 隐藏消息窗。
/// 手势默认解析 Ctrl+Shift+V；解析失败时回退到该组合。
/// </summary>
public sealed class WindowsHotkeyService : IHotkeyService
{
    private const int HotkeyId = 0x4942; // 'IB'
    private const int WmHotkey = 0x0312;

    private Thread? _thread;
    private volatile bool _running;
    private IntPtr _hwnd;
    private string _gesture = "Ctrl+Shift+V";

    public event EventHandler? HotkeyPressed;

    public Task RegisterAsync(string gesture, CancellationToken cancellationToken = default)
    {
        _gesture = string.IsNullOrWhiteSpace(gesture) ? "Ctrl+Shift+V" : gesture.Trim();
        if (_running)
        {
            // 重新注册：先卸再建
            StopThread();
        }

        StartThread();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        StopThread();
        return ValueTask.CompletedTask;
    }

    private void StartThread()
    {
        _running = true;
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "Inkboard.Hotkey",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void StopThread()
    {
        _running = false;
        if (_hwnd != IntPtr.Zero)
            PostMessage(_hwnd, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
        _thread?.Join(1000);
        _thread = null;
        _hwnd = IntPtr.Zero;
    }

    private void MessageLoop()
    {
        _hwnd = CreateMessageWindow();
        ParseGesture(_gesture, out var mods, out var key);
        if (!RegisterHotKey(_hwnd, HotkeyId, mods, key))
        {
            // 注册失败时仍保活线程，避免反复崩溃；上层可依赖窗口内兜底快捷键
            mods = ModControl | ModShift;
            key = 0x56; // V
            RegisterHotKey(_hwnd, HotkeyId, mods, key);
        }

        while (_running && GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            if (msg.Message == WmHotkey && msg.WParam == (IntPtr)HotkeyId)
                HotkeyPressed?.Invoke(this, EventArgs.Empty);

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        UnregisterHotKey(_hwnd, HotkeyId);
        DestroyWindow(_hwnd);
        _hwnd = IntPtr.Zero;
    }

    private static void ParseGesture(string gesture, out uint mods, out uint key)
    {
        mods = 0;
        key = 0x56;
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    mods |= ModControl;
                    break;
                case "shift":
                    mods |= ModShift;
                    break;
                case "alt":
                    mods |= ModAlt;
                    break;
                case "win":
                case "meta":
                    mods |= ModWin;
                    break;
                default:
                    if (part.Length == 1)
                        key = char.ToUpperInvariant(part[0]);
                    break;
            }
        }
    }

    private static IntPtr CreateMessageWindow()
    {
        var wc = new WndClassEx
        {
            CbSize = Marshal.SizeOf<WndClassEx>(),
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(DefWindowProc),
            HInstance = GetModuleHandle(null),
            LpszClassName = "InkboardHotkeyHiddenWindow",
        };
        RegisterClassEx(ref wc);
        return CreateWindowEx(0, wc.LpszClassName, string.Empty, 0,
            0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wc.HInstance, IntPtr.Zero);
    }

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public int CbSize;
        public int Style;
        public IntPtr LpfnWndProc;
        public int CbClsExtra;
        public int CbWndExtra;
        public IntPtr HInstance;
        public IntPtr HIcon;
        public IntPtr HCursor;
        public IntPtr HbrBackground;
        public string LpszMenuName;
        public string LpszClassName;
        public IntPtr HIconSm;
    }
}
