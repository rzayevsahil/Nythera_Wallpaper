using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nythera;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    public static Core.Performance.SmartPerformanceManager PerformanceManager { get; } = new();
    public static Core.Interactive.MouseTrackerService MouseTracker { get; } = new();
    public static Core.Interactive.CharacterController CharacterController { get; private set; }
    public static Core.Audio.AudioCaptureService AudioCapture { get; } = new();
    public static Core.Audio.AudioAnalyzer AudioAnalyzer { get; private set; }
    public static Core.Audio.AudioReactiveController AudioController { get; private set; }
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    private H.NotifyIcon.TaskbarIcon _trayIcon;
    public MenuFlyoutItem _dashboardItem;
    public MenuFlyoutItem _quitItem;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // Set safe WebView2 User Data Folder (UDF) for unpackaged WinUI 3 app
            string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string udfPath = System.IO.Path.Combine(localAppData, "Nythera", "WebView2UserData");
            System.IO.Directory.CreateDirectory(udfPath);
            System.Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", udfPath);

            PerformanceManager.Start();
            CharacterController = new Core.Interactive.CharacterController(MouseTracker);
            MouseTracker.Start();
            AudioAnalyzer = new Core.Audio.AudioAnalyzer(AudioCapture);
            AudioController = new Core.Audio.AudioReactiveController(AudioAnalyzer);
            AudioController.Start();
            _window = new MainWindow();
            
            bool isHidden = System.Linq.Enumerable.Contains(System.Environment.GetCommandLineArgs(), "--hidden");
        if (isHidden)
        {
            // WinUI 3 requires the window to be activated at least once to initialize XamlRoot.
            // If started hidden (e.g. from startup), move it off-screen, activate to get XamlRoot, then hide.
            _window.AppWindow.Move(new Windows.Graphics.PointInt32(-32000, -32000));
            _window.Activate();
            _window.AppWindow.Hide();
            
            // Move it back to center for when the user eventually opens it
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(_window.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                int x = (displayArea.WorkArea.Width - _window.AppWindow.Size.Width) / 2;
                int y = (displayArea.WorkArea.Height - _window.AppWindow.Size.Height) / 2;
                _window.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
        }
        else
        {
            _window.Activate();
        }
        
        // Initialize tray icon in code-behind
        var menuFlyout = new MenuFlyout();
        
        _dashboardItem = new MenuFlyoutItem { Text = Services.LocalizationService.GetString("TrayDashboard") };
        _dashboardItem.Command = new RelayCommand(() =>
        {
            if (_window != null)
            {
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(_window.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    int x = (displayArea.WorkArea.Width - _window.AppWindow.Size.Width) / 2;
                    int y = (displayArea.WorkArea.Height - _window.AppWindow.Size.Height) / 2;
                    _window.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
                }
                _window.AppWindow.Show();
                _window.Activate();
            }
        });
        menuFlyout.Items.Add(_dashboardItem);
        
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        
        _quitItem = new MenuFlyoutItem { Text = Services.LocalizationService.GetString("TrayQuit") };
        _quitItem.Command = new RelayCommand(() =>
        {
            _trayIcon?.Dispose();
            Core.WallpaperEngine.DesktopInterop.RestoreDesktop();
            Environment.Exit(0);
        });
        menuFlyout.Items.Add(_quitItem);

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "Nythera",
            IconSource = new BitmapImage(new System.Uri("ms-appx:///Assets/AppIcon.ico")),
            ContextFlyout = menuFlyout
        };
        _trayIcon.ForceCreate();
        
        // XamlRoot must be set on the flyout for click events to fire in WinUI 3.
        if (_window.Content?.XamlRoot != null)
        {
            menuFlyout.XamlRoot = _window.Content.XamlRoot;
        }

        // Hook into window's Activated event just in case it takes a moment
        _window.Activated += (s, e) =>
        {
            if (_window.Content?.XamlRoot != null && menuFlyout.XamlRoot == null)
            {
                menuFlyout.XamlRoot = _window.Content.XamlRoot;
            }
        };
        
        System.AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("app_crash.txt", ex.ToString());
            throw;
        }
    }

    private void TrayDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (_window != null)
        {
            _window.AppWindow.Show();
            _window.Activate();
        }
    }

    private void TrayQuit_Click(object sender, RoutedEventArgs e)
    {
        _trayIcon?.Dispose();
        PerformanceManager?.Stop();
        MouseTracker?.Stop();
        AudioController?.Stop();
        Core.WallpaperEngine.DesktopInterop.RestoreDesktop();
        Environment.Exit(0);
    }

    public void UpdateTrayLanguage()
    {
        if (_dashboardItem != null)
            _dashboardItem.Text = Services.LocalizationService.GetString("TrayDashboard");
        if (_quitItem != null)
            _quitItem.Text = Services.LocalizationService.GetString("TrayQuit");
    }

    private void CurrentDomain_ProcessExit(object? sender, System.EventArgs e)
    {
        PerformanceManager?.Stop();
        MouseTracker?.Stop();
        AudioController?.Stop();
        Core.WallpaperEngine.DesktopInterop.RestoreDesktop();
    }
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly System.Action _execute;
    public RelayCommand(System.Action execute) => _execute = execute;
    public event System.EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
