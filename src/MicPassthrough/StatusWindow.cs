using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

/// <summary>
/// Status window for daemon mode showing real-time passthrough status and recent logs.
/// Displayed when double-clicking the tray icon.
/// </summary>
public partial class StatusWindow : Form
{
    private readonly ILogger _logger;
    private Label statusLabel;
    private Label statusValueLabel;
    private Label micLabel;
    private Label cableLabel;
    private TextBox logBox;
    private Button toggleButton;
    private Button closeButton;
    private Queue<string> logHistory;
    private const int MaxLogLines = 100;

    public event EventHandler<EventArgs> ToggleRequested;
    public event EventHandler<string> ModeRequested;

    public StatusWindow(ILogger logger)
    {
        _logger = logger;
        logHistory = new Queue<string>();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Microphone Passthrough - Status";
        this.Width = 600;
        this.Height = 500;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Icon = SystemIcons.Application;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(500, 400);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(10),
            AutoSize = false,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        // Set up row and column styles
        // Rows 0-3: Auto size (status, mic, cable, log title)
        // Row 4: Expand to fill remaining space
        // Row 5: Auto size (buttons)
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 0
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 1
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 2
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 3
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Row 4 - expand
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Row 5

        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));  // Column 0
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));  // Column 1

        // Status section
        statusLabel = new Label
        {
            Text = "Passthrough Status:",
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };

        statusValueLabel = new Label
        {
            Text = "Inactive",
            AutoSize = true,
            ForeColor = Color.Red,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 12, FontStyle.Bold)
        };

        mainPanel.Controls.Add(statusLabel, 0, 0);
        mainPanel.Controls.Add(statusValueLabel, 1, 0);

        // Mic device
        micLabel = new Label
        {
            Text = "Microphone: (detecting...)",
            AutoSize = true
        };
        mainPanel.Controls.Add(micLabel, 0, 1);

        // Cable device
        cableLabel = new Label
        {
            Text = "Cable Output: (detecting...)",
            AutoSize = true
        };
        mainPanel.Controls.Add(cableLabel, 0, 2);

        // Logs section
        var logTitleLabel = new Label
        {
            Text = "Recent Activity:",
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };
        mainPanel.Controls.Add(logTitleLabel, 0, 3);

        logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Courier New", 9),
            Height = 200  // Take up remaining vertical space
        };
        mainPanel.Controls.Add(logBox, 0, 4);
        mainPanel.SetColumnSpan(logBox, 2);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        // Mode label
        var modeLabel = new Label
        {
            Text = "Mode:",
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        };
        
        // Mode buttons
        var enabledModeButton = new Button
        {
            Text = "Enabled",
            Width = 90,
            Height = 25
        };
        enabledModeButton.Click += (s, e) => ModeRequested?.Invoke(this, "enabled");
        
        var autoSwitchModeButton = new Button
        {
            Text = "Auto-Switch",
            Width = 90,
            Height = 25
        };
        autoSwitchModeButton.Click += (s, e) => ModeRequested?.Invoke(this, "auto-switch");
        
        var disabledModeButton = new Button
        {
            Text = "Disabled",
            Width = 90,
            Height = 25
        };
        disabledModeButton.Click += (s, e) => ModeRequested?.Invoke(this, "disabled");

        closeButton = new Button
        {
            Text = "Close",
            Width = 100,
            Height = 30
        };
        closeButton.Click += (s, e) => this.Close();

        toggleButton = new Button
        {
            Text = "Start Passthrough",
            Width = 150,
            Height = 30
        };
        toggleButton.Click += (s, e) => ToggleRequested?.Invoke(this, EventArgs.Empty);

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(toggleButton);
        buttonPanel.Controls.Add(disabledModeButton);
        buttonPanel.Controls.Add(autoSwitchModeButton);
        buttonPanel.Controls.Add(enabledModeButton);
        buttonPanel.Controls.Add(modeLabel);

        mainPanel.Controls.Add(buttonPanel, 0, 5);
        mainPanel.SetColumnSpan(buttonPanel, 2);

        this.Controls.Add(mainPanel);
    }

    public void SetStatus(bool isActive)
    {
        if (statusValueLabel.InvokeRequired)
        {
            statusValueLabel.Invoke(() => SetStatus(isActive));
            return;
        }

        if (isActive)
        {
            statusValueLabel.Text = "Active";
            statusValueLabel.ForeColor = Color.Green;
            toggleButton.Text = "Stop Passthrough";
        }
        else
        {
            statusValueLabel.Text = "Inactive";
            statusValueLabel.ForeColor = Color.Red;
            toggleButton.Text = "Start Passthrough";
        }
    }

    public void SetMicrophoneDevice(string deviceName)
    {
        if (micLabel.InvokeRequired)
        {
            micLabel.Invoke(() => SetMicrophoneDevice(deviceName));
            return;
        }

        micLabel.Text = $"Microphone: {deviceName}";
    }

    public void SetCableDevice(string deviceName)
    {
        if (cableLabel.InvokeRequired)
        {
            cableLabel.Invoke(() => SetCableDevice(deviceName));
            return;
        }

        cableLabel.Text = $"Cable Output: {deviceName}";
    }

    public void AddLog(string message)
    {
        if (logBox.InvokeRequired)
        {
            logBox.Invoke(() => AddLog(message));
            return;
        }

        logHistory.Enqueue(message);
        if (logHistory.Count > MaxLogLines)
            logHistory.Dequeue();

        logBox.Text = string.Join(Environment.NewLine, logHistory);
        logBox.SelectionStart = logBox.Text.Length;
        logBox.ScrollToCaret();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
        }
        else
        {
            base.OnFormClosing(e);
        }
    }
}
