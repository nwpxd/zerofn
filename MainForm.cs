using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroFN;

public class MainForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(26, 26, 46);
    private static readonly Color PanelColor = Color.FromArgb(22, 33, 62);
    private static readonly Color AccentColor = Color.FromArgb(15, 52, 96);
    private static readonly Color CyanAccent = Color.FromArgb(0, 210, 255);

    private readonly ComboBox _lootKeyCombo;
    private readonly ComboBox _editKeyCombo;
    private readonly ToggleSwitch _lootToggle;
    private readonly ToggleSwitch _editToggle;
    private readonly Label _statusLabel;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;

    private AppConfig _config = null!;
    private InputEngine? _engine;

    public MainForm()
    {
        // Form setup
        Text = "ZeroFN";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgColor;
        ClientSize = new Size(320, 400);

        // Title
        var titleLabel = new Label
        {
            Text = "ZeroFN",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(0, 8, 0, 0),
        };
        Controls.Add(titleLabel);

        // Subtitle
        var subtitleLabel = new Label
        {
            Text = "Kernel-level input macros",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(140, 140, 160),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 22,
        };
        Controls.Add(subtitleLabel);

        // Separator 1
        Controls.Add(CreateSeparator(70));

        // Loot section
        var lootPanel = CreateMacroSection("LOOT", "Key held → rapid key repeat", out _lootKeyCombo, out _lootToggle, 82);
        Controls.Add(lootPanel);

        // Edit section
        var editPanel = CreateMacroSection("EDIT", "Key press → left click", out _editKeyCombo, out _editToggle, 202);
        Controls.Add(editPanel);

        // Separator 2
        Controls.Add(CreateSeparator(320));

        // Status label
        _statusLabel = new Label
        {
            Text = "Engine: Starting...",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(100, 255, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 332),
            Size = new Size(320, 20),
        };
        Controls.Add(_statusLabel);

        // Minimize to tray button
        var minimizeBtn = new Button
        {
            Text = "Minimize to Tray",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(180, 180, 200),
            BackColor = AccentColor,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(90, 360),
            Size = new Size(140, 28),
            Cursor = Cursors.Hand,
        };
        minimizeBtn.FlatAppearance.BorderColor = AccentColor;
        minimizeBtn.Click += (_, _) => HideToTray();
        Controls.Add(minimizeBtn);

        // Tray icon
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Show", null, (_, _) => ShowFromTray());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "ZeroFN",
            ContextMenuStrip = _trayMenu,
            Visible = false,
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        // Events
        _lootToggle.CheckedChanged += OnLootToggleChanged;
        _editToggle.CheckedChanged += OnEditToggleChanged;
        _lootKeyCombo.SelectedIndexChanged += OnLootKeyChanged;
        _editKeyCombo.SelectedIndexChanged += OnEditKeyChanged;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        _config = AppConfig.Load();

        // Populate comboboxes
        var keys = ScanCodes.Map.Keys.OrderBy(k => k).ToArray();
        _lootKeyCombo.Items.AddRange(keys);
        _editKeyCombo.Items.AddRange(keys);

        _lootKeyCombo.SelectedItem = _config.Loot.KeyName;
        _editKeyCombo.SelectedItem = _config.Edit.KeyName;

        // Set toggles without triggering events
        _lootToggle.CheckedChanged -= OnLootToggleChanged;
        _editToggle.CheckedChanged -= OnEditToggleChanged;
        _lootToggle.Checked = _config.Loot.Enabled;
        _editToggle.Checked = _config.Edit.Enabled;
        _lootToggle.CheckedChanged += OnLootToggleChanged;
        _editToggle.CheckedChanged += OnEditToggleChanged;

        StartEngine();
    }

    private void StartEngine()
    {
        try
        {
            ushort lootCode = ScanCodes.Map.TryGetValue(_config.Loot.KeyName, out var lc) ? lc : ScanCodes.Map["E"];
            ushort editCode = ScanCodes.Map.TryGetValue(_config.Edit.KeyName, out var ec) ? ec : ScanCodes.Map["F"];

            _engine = new InputEngine(lootCode, editCode, _config.Loot.Enabled, _config.Edit.Enabled);
            _engine.Start();

            _statusLabel.Text = "Engine: Running";
            _statusLabel.ForeColor = Color.FromArgb(100, 255, 100);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Engine: Stopped";
            _statusLabel.ForeColor = Color.FromArgb(255, 80, 80);
            MessageBox.Show(
                $"Failed to start input engine:\n\n{ex.Message}",
                "ZeroFN — Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Panel CreateMacroSection(string title, string subtitle, out ComboBox combo, out ToggleSwitch toggle, int yOffset)
    {
        var panel = new Panel
        {
            Location = new Point(16, yOffset),
            Size = new Size(288, 110),
            BackColor = PanelColor,
        };

        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(12, 8),
            AutoSize = true,
        };
        panel.Controls.Add(titleLbl);

        var subLbl = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(140, 140, 160),
            Location = new Point(12, 30),
            AutoSize = true,
        };
        panel.Controls.Add(subLbl);

        combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(30, 40, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(12, 58),
            Size = new Size(160, 30),
        };
        panel.Controls.Add(combo);

        toggle = new ToggleSwitch
        {
            Location = new Point(220, 62),
        };
        panel.Controls.Add(toggle);

        return panel;
    }

    private Panel CreateSeparator(int y)
    {
        return new Panel
        {
            Location = new Point(16, y),
            Size = new Size(288, 1),
            BackColor = Color.FromArgb(50, 50, 70),
        };
    }

    private void OnLootToggleChanged(object? sender, EventArgs e)
    {
        _config.Loot.Enabled = _lootToggle.Checked;
        if (_engine != null) _engine.LootEnabled = _lootToggle.Checked;
        AppConfig.Save(_config);
    }

    private void OnEditToggleChanged(object? sender, EventArgs e)
    {
        _config.Edit.Enabled = _editToggle.Checked;
        if (_engine != null) _engine.EditEnabled = _editToggle.Checked;
        AppConfig.Save(_config);
    }

    private void OnLootKeyChanged(object? sender, EventArgs e)
    {
        var key = _lootKeyCombo.SelectedItem?.ToString();
        if (key != null && ScanCodes.Map.TryGetValue(key, out ushort code))
        {
            _config.Loot.KeyName = key;
            if (_engine != null) _engine.LootScanCode = code;
            AppConfig.Save(_config);
        }
    }

    private void OnEditKeyChanged(object? sender, EventArgs e)
    {
        var key = _editKeyCombo.SelectedItem?.ToString();
        if (key != null && ScanCodes.Map.TryGetValue(key, out ushort code))
        {
            _config.Edit.KeyName = key;
            if (_engine != null) _engine.EditScanCode = code;
            AppConfig.Save(_config);
        }
    }

    private void HideToTray()
    {
        Hide();
        _trayIcon.Visible = true;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        _trayIcon.Visible = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    private void ExitApplication()
    {
        _engine?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        Application.Exit();
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(15, 52, 96));
        using var font = new Font("Segoe UI", 9, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(0, 210, 255));
        g.DrawString("Z", font, brush, -1, 0);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine?.Dispose();
            _trayIcon?.Dispose();
            _trayMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
