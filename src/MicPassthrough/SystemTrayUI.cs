using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
    /// Updates tray icon and menu state.
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
    /// Gets or sets the action to invoke when the tray icon is double-clicked.
    /// </summary>
    public Action DoubleClickAction { get; set; }

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

        // Create tray icon with application icon
        _trayIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
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
    /// Loads the application icon from the project assets.
    /// Falls back to system icon if not found.
    /// Supports multiple paths for dotnet run, published builds, and development scenarios.
    /// </summary>
    private Icon LoadApplicationIcon()
    {
        try
        {
            // Try multiple strategies to locate the icon
            var searchPaths = new List<string>();

            // Strategy 1: Look in same directory as exe (published/release builds)
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                var exeDir = Path.GetDirectoryName(exePath);
                searchPaths.Add(Path.Combine(exeDir, "icon.ico"));
                
                // Strategy 2: From bin/Debug/net10.0-windows/ → docs/assets/ (for published builds)
                searchPaths.Add(Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "docs", "assets", "icon.ico")));
                
                // Strategy 3: From src/MicPassthrough/bin/Debug/ → workspace/docs/assets/ (for builds with source structure)
                searchPaths.Add(Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "docs", "assets", "icon.ico")));
            }

            // Strategy 4: From current working directory (for dotnet run scenarios)
            searchPaths.Add(Path.GetFullPath("docs/assets/icon.ico"));
            searchPaths.Add(Path.GetFullPath("../docs/assets/icon.ico"));
            searchPaths.Add(Path.GetFullPath("../../docs/assets/icon.ico"));
            searchPaths.Add(Path.GetFullPath("../../../docs/assets/icon.ico"));

            foreach (var iconPath in searchPaths)
            {
                if (File.Exists(iconPath))
                {
                    _logger.LogInformation("Loaded tray icon from: {IconPath}", iconPath);
                    return new Icon(iconPath);
                }
            }

            _logger.LogWarning("Could not find icon.ico in any expected location, using system icon");
            _logger.LogDebug("Searched {Count} paths for icon.ico", searchPaths.Count);
            return SystemIcons.Application;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading application icon, using system icon");
            return SystemIcons.Application;
        }
    }

    private void OnStartClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Start passthrough requested from tray menu");
        StartRequested?.Invoke(this, EventArgs.Empty);
        IsPassthroughActive = true;
        ShowNotification("Microphone Passthrough", "Passthrough activated");
    }

    private void OnStopClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Stop passthrough requested from tray menu");
        StopRequested?.Invoke(this, EventArgs.Empty);
        IsPassthroughActive = false;
        ShowNotification("Microphone Passthrough", "Passthrough deactivated");
    }

    private void OnExitClick(object sender, EventArgs e)
    {
        _logger.LogInformation("Exit requested from tray menu");
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnTrayIconDoubleClick(object sender, EventArgs e)
    {
        _logger.LogDebug("Tray icon double-clicked - invoking double-click action");
        // Invoke the double-click action (typically showing the status window)
        DoubleClickAction?.Invoke();
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
