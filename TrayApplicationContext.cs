using System.Runtime.InteropServices;

namespace AudioSwitcher;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private AppSettings _settings;
    private GlobalHotkey? _hotkey;
    private List<AudioDeviceManager.AudioDevice> _devices = new();
    private HiddenWindow? _hiddenWindow;
    private Dictionary<string, ToolStripMenuItem> _deviceMenuItems = new();

    public TrayApplicationContext()
    {
        _settings = SettingsManager.Load();
        
        // Create hidden window for hotkey messages
        _hiddenWindow = new HiddenWindow();
        _hiddenWindow.HotkeyPressed += OnHotkeyPressed;

        // Initialize hotkey
        _hotkey = new GlobalHotkey(_hiddenWindow.Handle);
        RegisterHotkey();

        // Create system tray icon
        InitializeTrayIcon();
        
        // Build menu
        RefreshDevices();
    }

    private void InitializeTrayIcon()
    {
        _contextMenu = new ContextMenuStrip();
        
        // Prevent menu from closing when toggling favorites
        _contextMenu.Closing += ContextMenu_Closing;
        
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Audio Device Switcher",
            ContextMenuStrip = _contextMenu
        };

        // Show menu on left-click
        _trayIcon.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                UpdateCurrentDeviceCheckmark();
                
                // Show the context menu
                var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                methodInfo?.Invoke(_trayIcon, null);
            }
        };
    }
    
    private void ContextMenu_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
        {
            e.Cancel = true;
        }
    }

    private void RefreshDevices()
    {
        _devices = AudioDeviceManager.GetPlaybackDevices();
        BuildMenu();
    }

    private void BuildMenu()
    {
        _contextMenu?.Items.Clear();
        _deviceMenuItems.Clear();

        if (_devices.Count == 0)
        {
            _contextMenu?.Items.Add(new ToolStripMenuItem("No audio devices found") { Enabled = false });
        }
        else
        {
            var currentDefault = AudioDeviceManager.GetDefaultPlaybackDevice();
            
            foreach (var device in _devices)
            {
                var item = new ToolStripMenuItem(device.Name);
                
                // Set checkmark if this is the current device
                item.Checked = currentDefault != null && device.Id == currentDefault.Id;
                
                // Make bold if this is a favorite
                if (_settings.FavoriteDeviceIds.Contains(device.Id))
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }
                
                // Add submenu for favorites management
                var isFavorite = _settings.FavoriteDeviceIds.Contains(device.Id);
                
                if (isFavorite)
                {
                    var removeItem = new ToolStripMenuItem("Remove from Favorites");
                    removeItem.Click += (s, e) => ToggleFavorite(device);
                    item.DropDownItems.Add(removeItem);
                }
                else
                {
                    var addItem = new ToolStripMenuItem("Add to Favorites");
                    addItem.Click += (s, e) => ToggleFavorite(device);
                    
                    // Disable if already have 2 favorites
                    if (_settings.FavoriteDeviceIds.Count >= 2)
                    {
                        addItem.Enabled = false;
                        addItem.ToolTipText = "Maximum 2 favorites allowed";
                    }
                    
                    item.DropDownItems.Add(addItem);
                }
                
                // Prevent the device's dropdown submenu from closing on item click
                item.DropDownOpening += (s, e) =>
                {
                    if (item.DropDown != null)
                    {
                        item.DropDown.Closing -= DropDown_Closing;
                        item.DropDown.Closing += DropDown_Closing;
                    }
                };
                
                // Left-click to switch device (only when clicking the main item, not submenu)
                item.Click += (s, e) =>
                {
                    // Only switch if not opening submenu
                    if (item.DropDownItems.Count > 0 && !item.IsOnDropDown)
                    {
                        // Don't switch, let submenu open
                        return;
                    }
                };
                
                // Use MouseDown to detect left-click specifically
                item.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        SwitchToDevice(device);
                    }
                };
                
                _deviceMenuItems[device.Id] = item;
                _contextMenu?.Items.Add(item);
            }
        }

        _contextMenu?.Items.Add(new ToolStripSeparator());

        // Settings submenu
        var settingsMenu = new ToolStripMenuItem("Settings");
        var hotkeyItem = new ToolStripMenuItem($"Change Hotkey (Current: {GetHotkeyDisplayString()})");
        hotkeyItem.Click += (s, e) => 
        {
            _contextMenu?.Close();
            ShowHotkeySettings();
        };
        settingsMenu.DropDownItems.Add(hotkeyItem);
        
        // Prevent settings dropdown from closing
        settingsMenu.DropDownOpening += (s, e) =>
        {
            if (settingsMenu.DropDown != null)
            {
                settingsMenu.DropDown.Closing -= DropDown_Closing;
                settingsMenu.DropDown.Closing += DropDown_Closing;
            }
        };
        
        _contextMenu?.Items.Add(settingsMenu);

        _contextMenu?.Items.Add(new ToolStripSeparator());
        
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) =>
        {
            // Allow Exit to close the menu
            if (_contextMenu != null)
            {
                _contextMenu.Closing -= ContextMenu_Closing;
            }
            Exit();
        };
        _contextMenu?.Items.Add(exitItem);
    }
    
    private void DropDown_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
        {
            e.Cancel = true;
        }
    }

    private void UpdateCurrentDeviceCheckmark()
    {
        var currentDefault = AudioDeviceManager.GetDefaultPlaybackDevice();
        
        // Update checkmarks without rebuilding entire menu
        foreach (var kvp in _deviceMenuItems)
        {
            kvp.Value.Checked = currentDefault != null && kvp.Key == currentDefault.Id;
        }
    }

    private void SwitchToDevice(AudioDeviceManager.AudioDevice device)
    {
        if (AudioDeviceManager.SetDefaultPlaybackDevice(device.Id))
        {
            _trayIcon!.ShowBalloonTip(2000, "Audio Switched", $"Now playing through: {device.Name}", ToolTipIcon.Info);
            
            // Update checkmarks only (no flicker)
            UpdateCurrentDeviceCheckmark();
        }
        else
        {
            _trayIcon!.ShowBalloonTip(2000, "Error", "Failed to switch audio device", ToolTipIcon.Error);
        }
    }

    private void ToggleFavorite(AudioDeviceManager.AudioDevice device)
    {
        bool wasRemoved = false;
        
        if (_settings.FavoriteDeviceIds.Contains(device.Id))
        {
            _settings.FavoriteDeviceIds.Remove(device.Id);
            wasRemoved = true;
        }
        else
        {
            if (_settings.FavoriteDeviceIds.Count >= 2)
            {
                MessageBox.Show("You can only have up to 2 favorite devices.", 
                    "Maximum Favorites Reached", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }
            _settings.FavoriteDeviceIds.Add(device.Id);
        }

        SettingsManager.Save(_settings);
        
        // Update only the affected device's appearance without closing menu
        if (_deviceMenuItems.TryGetValue(device.Id, out var menuItem))
        {
            // Update font style
            if (wasRemoved)
            {
                menuItem.Font = new Font(menuItem.Font, FontStyle.Regular);
            }
            else
            {
                menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
            }
            
            // Update submenu items
            menuItem.DropDownItems.Clear();
            
            var isFavorite = _settings.FavoriteDeviceIds.Contains(device.Id);
            
            if (isFavorite)
            {
                var removeItem = new ToolStripMenuItem("Remove from Favorites");
                removeItem.Click += (s, e) => ToggleFavorite(device);
                menuItem.DropDownItems.Add(removeItem);
            }
            else
            {
                var addItem = new ToolStripMenuItem("Add to Favorites");
                addItem.Click += (s, e) => ToggleFavorite(device);
                
                // Disable if already have 2 favorites
                if (_settings.FavoriteDeviceIds.Count >= 2)
                {
                    addItem.Enabled = false;
                    addItem.ToolTipText = "Maximum 2 favorites allowed";
                }
                
                menuItem.DropDownItems.Add(addItem);
            }
            
            // If we now have 2 favorites, update all other non-favorite items to disable "Add to Favorites"
            if (_settings.FavoriteDeviceIds.Count == 2 && !wasRemoved)
            {
                foreach (var kvp in _deviceMenuItems)
                {
                    if (!_settings.FavoriteDeviceIds.Contains(kvp.Key) && kvp.Value.DropDownItems.Count > 0)
                    {
                        var addToFavItem = kvp.Value.DropDownItems[0] as ToolStripMenuItem;
                        if (addToFavItem != null && addToFavItem.Text == "Add to Favorites")
                        {
                            addToFavItem.Enabled = false;
                        }
                    }
                }
            }
            // If we went from 2 to 1 favorite, re-enable "Add to Favorites" for non-favorites
            else if (_settings.FavoriteDeviceIds.Count == 1 && wasRemoved)
            {
                foreach (var kvp in _deviceMenuItems)
                {
                    if (!_settings.FavoriteDeviceIds.Contains(kvp.Key) && kvp.Value.DropDownItems.Count > 0)
                    {
                        var addToFavItem = kvp.Value.DropDownItems[0] as ToolStripMenuItem;
                        if (addToFavItem != null && addToFavItem.Text == "Add to Favorites")
                        {
                            addToFavItem.Enabled = true;
                        }
                    }
                }
            }
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        ToggleFavorites();
    }

    private void ToggleFavorites()
    {
        if (_settings.FavoriteDeviceIds.Count != 2)
        {
            // Do nothing if we don't have exactly 2 favorites
            return;
        }

        var currentDefault = AudioDeviceManager.GetDefaultPlaybackDevice();
        if (currentDefault == null) return;

        // Find which favorite to switch to
        string targetDeviceId;
        if (currentDefault.Id == _settings.FavoriteDeviceIds[0])
        {
            targetDeviceId = _settings.FavoriteDeviceIds[1];
        }
        else
        {
            targetDeviceId = _settings.FavoriteDeviceIds[0];
        }

        // Find the device
        var targetDevice = _devices.FirstOrDefault(d => d.Id == targetDeviceId);
        if (targetDevice != null)
        {
            if (AudioDeviceManager.SetDefaultPlaybackDevice(targetDevice.Id))
            {
                _trayIcon!.ShowBalloonTip(2000, "Audio Switched", $"Now playing through: {targetDevice.Name}", ToolTipIcon.Info);
                
                // Update checkmarks only
                UpdateCurrentDeviceCheckmark();
            }
        }
    }

    private void RegisterHotkey()
    {
        var modifiers = GlobalHotkey.GetModifiers(_settings.HotkeyCtrl, _settings.HotkeyAlt, _settings.HotkeyShift);
        var key = GlobalHotkey.ParseKey(_settings.HotkeyKey);
        _hotkey?.Register(modifiers, key);
    }

    private string GetHotkeyDisplayString()
    {
        var parts = new List<string>();
        if (_settings.HotkeyCtrl) parts.Add("Ctrl");
        if (_settings.HotkeyAlt) parts.Add("Alt");
        if (_settings.HotkeyShift) parts.Add("Shift");
        parts.Add(_settings.HotkeyKey);
        return string.Join("+", parts);
    }

    private void ShowHotkeySettings()
    {
        using var form = new HotkeySettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings = form.Settings;
            SettingsManager.Save(_settings);
            RegisterHotkey();
            
            // Rebuild menu to show new hotkey in Settings
            RefreshDevices();
        }
    }

    private void Exit()
    {
        _hotkey?.Dispose();
        _hiddenWindow?.Dispose();
        _trayIcon!.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // Hidden window to receive hotkey messages
    private class HiddenWindow : Form
    {
        public event EventHandler? HotkeyPressed;

        public HiddenWindow()
        {
            // Make the window invisible and not shown in taskbar
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Width = 0;
            Height = 0;
            Opacity = 0;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }
    }
}