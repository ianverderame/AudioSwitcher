namespace AudioSwitcher;

public class HotkeySettingsForm : Form
{
    private ComboBox? _keyComboBox;
    private CheckBox? _ctrlCheckBox;
    private CheckBox? _altCheckBox;
    private CheckBox? _shiftCheckBox;
    private Button? _okButton;
    private Button? _cancelButton;

    public AppSettings Settings { get; private set; }

    public HotkeySettingsForm(AppSettings currentSettings)
    {
        Settings = new AppSettings
        {
            FavoriteDeviceIds = new List<string>(currentSettings.FavoriteDeviceIds),
            HotkeyKey = currentSettings.HotkeyKey,
            HotkeyCtrl = currentSettings.HotkeyCtrl,
            HotkeyAlt = currentSettings.HotkeyAlt,
            HotkeyShift = currentSettings.HotkeyShift
        };

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Hotkey Settings";
        Width = 350;
        Height = 220;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Key selection
        var keyLabel = new Label
        {
            Text = "Key:",
            Left = 20,
            Top = 20,
            Width = 100
        };
        Controls.Add(keyLabel);

        _keyComboBox = new ComboBox
        {
            Left = 130,
            Top = 20,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // Add common function keys and letters
        var keys = new[] { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
        foreach (var key in keys)
        {
            _keyComboBox.Items.Add(key);
        }
        
        _keyComboBox.SelectedItem = Settings.HotkeyKey;
        Controls.Add(_keyComboBox);

        // Modifiers
        var modifiersLabel = new Label
        {
            Text = "Modifiers:",
            Left = 20,
            Top = 60,
            Width = 100
        };
        Controls.Add(modifiersLabel);

        _ctrlCheckBox = new CheckBox
        {
            Text = "Ctrl",
            Left = 130,
            Top = 60,
            Width = 60,
            Checked = Settings.HotkeyCtrl
        };
        Controls.Add(_ctrlCheckBox);

        _altCheckBox = new CheckBox
        {
            Text = "Alt",
            Left = 200,
            Top = 60,
            Width = 60,
            Checked = Settings.HotkeyAlt
        };
        Controls.Add(_altCheckBox);

        _shiftCheckBox = new CheckBox
        {
            Text = "Shift",
            Left = 270,
            Top = 60,
            Width = 60,
            Checked = Settings.HotkeyShift
        };
        Controls.Add(_shiftCheckBox);

        // Info label
        var infoLabel = new Label
        {
            Text = "This hotkey will toggle between your 2 favorite devices.\n(Only works when you have exactly 2 favorites set)",
            Left = 20,
            Top = 100,
            Width = 300,
            Height = 40
        };
        Controls.Add(infoLabel);

        // Buttons
        _okButton = new Button
        {
            Text = "OK",
            Left = 140,
            Top = 150,
            Width = 80,
            DialogResult = DialogResult.OK
        };
        _okButton.Click += OkButton_Click;
        Controls.Add(_okButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Left = 230,
            Top = 150,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        Settings.HotkeyKey = _keyComboBox?.SelectedItem?.ToString() ?? "F11";
        Settings.HotkeyCtrl = _ctrlCheckBox?.Checked ?? false;
        Settings.HotkeyAlt = _altCheckBox?.Checked ?? false;
        Settings.HotkeyShift = _shiftCheckBox?.Checked ?? false;
    }
}