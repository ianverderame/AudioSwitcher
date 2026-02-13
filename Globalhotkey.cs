using System.Runtime.InteropServices;

namespace AudioSwitcher;

public class GlobalHotkey : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    // Modifiers
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    private readonly IntPtr _handle;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public GlobalHotkey(IntPtr windowHandle)
    {
        _handle = windowHandle;
    }

    public bool Register(uint modifiers, Keys key)
    {
        Unregister();
        _registered = RegisterHotKey(_handle, HOTKEY_ID, modifiers, (uint)key);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_handle, HOTKEY_ID);
            _registered = false;
        }
    }

    public void ProcessHotkey(Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        Unregister();
    }

    public static uint GetModifiers(bool ctrl, bool alt, bool shift)
    {
        uint modifiers = 0;
        if (ctrl) modifiers |= MOD_CONTROL;
        if (alt) modifiers |= MOD_ALT;
        if (shift) modifiers |= MOD_SHIFT;
        return modifiers;
    }

    public static Keys ParseKey(string keyString)
    {
        if (Enum.TryParse<Keys>(keyString, true, out var key))
        {
            return key;
        }
        return Keys.F11; // Default fallback
    }
}