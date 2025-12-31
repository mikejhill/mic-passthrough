using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.Logging;

/// <summary>
/// System tray UI component for daemon mode.
/// Provides system tray icon with status indicator and context menu controls.
/// </summary>
public class SystemTrayUI : IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private readonly ILogger _logger;
    private readonly ContextMenuStrip _contextMenu;
    private bool _isPassthroughActive;
    private bool _disposed;

    /// <summary>
    /// Gets or sets whether passthrough is currently active.
    /// Updates tray icon tooltip and menu state.
    /// </summary>
    public bool IsPassthroughActive
    {
        get => _isPassthroughActive;
        set
        {
            _isPassthroughActive = value;
            UpdateTrayIcon();
        }
    }

    /// <summary>
    /// Gets or sets the microphone device name displayed in tooltip.
    /// </summary>
    public string MicrophoneDevice { get; set; }

    /// <summary>
    /// Gets or sets the cable output device name displayed in tooltip.
    /// </summary>
    public string CableDevice { get; set; }

    /// <summary>
    /// Occurs when user clicks "Start Passthrough" menu item.
    /// </summary>
    public event EventHandler StartRequested;

    /// <summary>
    /// Occurs when user clicks "Stop Passthrough" menu item.
    /// </summary>
    public event EventHandler StopRequested;

    /// <summary>
    /// Occurs when user clicks "Exit" menu item.
    /// </summary>
    public event EventHandler ExitRequested;

    /// <summary>
    /// Initializes the system tray UI with icon and context menu.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public SystemTrayUI(ILogger logger)
    {
        _logger = logger;
        _isPassthroughActive = false;

        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Start Passthrough", null, OnStartClick);
        _contextMenu.Items.Add("Stop Passthrough", null, OnStopClick);
        _contextMenu.Items.Add("-"); // Separator
        _contextMenu.Items.Add("Exit", null, OnExitClick);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "Microphone Passthrough",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += OnTrayIconDoubleClick;

        _logger.LogInformation("System tray UI initialized");
    }

    /// <summary>
    /// Shows a balloon notification in the system tray.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message.</param>
    /// <param name="duration">Display duration in milliseconds.</param>
    public void ShowNotification(string title, string message, int duration = 5000)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = message;
        _trayIcon.ShowBalloonTip(duration);
    }

    /// <summary>
    /// Updates the tray icon and tooltip based on current state.
    /// </summary>
    private void UpdateTrayIcon()
    {
        var status = _isPassthroughActive ? "Active" : "Inactive";
        var tooltip = $"Microphone Passthrough\nStatus: {status}";

        if (!string.IsNullOrEmpty(MicrophoneDevice))
            tooltip += $"\nMic: {MicrophoneDevice}";

        if (!string.IsNullOrEmpty(CableDevice))
            tooltip += $"\nCable: {CableDevice}";

        _trayIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip.Substring(0, 60) + "...";

        // Update menu items
        if (_contextMenu.Items.Count >= 2)
        {
            ((ToolStripItem)_contextMenu.Items[0]).Enabled = !_isPassthroughActive;
            ((ToolStripItem)_contextMenu.Items[1]).Enabled = _isPassthroughActive;
        }

        _logger.LogDebug("Tray icon updated: {Status}", status);
    }

    /// <summary>
    /// Creates a simple default icon (colored circle) for the tray.
    /// In production, this would use an embedded resource icon.
    /// </summary>
#pragma warning disable CA1416 // Validate platform compatibility - Daemon mode is Windows-only
    private Icon CreateDefaultIcon()
    {
        // Create a simple 16x16 icon using a Bitmap
        var bitmap = new Bitmap(16, 16);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(SystemColors.Control);
            graphics.FillEllipse(Brushes.Green, 0, 0, 15, 15);
            graphics.DrawEllipse(Pens.Black, 0, 0, 15, 15);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }
#pragma warning restore CA1416

    private void OnStartClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Start passthrough requested from tray menu");
        StartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Stop passthrough requested from tray menu");
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnExitClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Exit requested from tray menu");
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnTrayIconDoubleClick(object sender, EventArgs e)
    {
        _logger.LogDebug("Tray icon double-clicked");
        ShowNotification("Microphone Passthrough",
            $"Status: {(_isPassthroughActive ? "Active" : "Inactive")}\n" +
            $"Mic: {MicrophoneDevice ?? "Unknown"}\n" +
            $"Cable: {CableDevice ?? "Unknown"}");
    }

    /// <summary>
    /// Cleans up tray icon resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _trayIcon?.Dispose();
        _contextMenu?.Dispose();
        _disposed = true;

        _logger.LogInformation("System tray UI disposed");
    }
}
