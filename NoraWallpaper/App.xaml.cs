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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NoraWallpaper;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    
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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        
        bool isHidden = System.Linq.Enumerable.Contains(System.Environment.GetCommandLineArgs(), "--hidden");
        if (!isHidden)
        {
            _window.Activate();
        }
        
        // Initialize tray icon in code-behind
        var menuFlyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();
        
        var dashboardItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Dashboard" };
        dashboardItem.Click += TrayDashboard_Click;
        menuFlyout.Items.Add(dashboardItem);
        
        menuFlyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());
        
        var quitItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quit" };
        quitItem.Click += TrayQuit_Click;
        menuFlyout.Items.Add(quitItem);

        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "Nora Wallpaper Engine",
            IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri("ms-appx:///Assets/AppIcon.ico")),
            ContextFlyout = menuFlyout
        };
        _trayIcon.ForceCreate();
        
        System.AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
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
        Core.WallpaperEngine.DesktopInterop.RestoreDesktop();
        Environment.Exit(0);
    }

    private void CurrentDomain_ProcessExit(object? sender, System.EventArgs e)
    {
        Core.WallpaperEngine.DesktopInterop.RestoreDesktop();
    }
}
