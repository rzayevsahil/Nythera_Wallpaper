using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nythera;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }
    
    public event System.EventHandler DisplayChanged;

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [System.Runtime.InteropServices.DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [System.Runtime.InteropServices.DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private SubclassProc _subclassDelegate;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        AppWindow.Closing += AppWindow_Closing;

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _subclassDelegate = new SubclassProc(WindowSubClass);
        SetWindowSubclass(hwnd, _subclassDelegate, (UIntPtr)1, IntPtr.Zero);

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    private IntPtr WindowSubClass(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        const uint WM_DISPLAYCHANGE = 0x007E;
        if (uMsg == WM_DISPLAYCHANGE)
        {
            // Invoke on UI thread safely
            this.DispatcherQueue.TryEnqueue(() =>
            {
                DisplayChanged?.Invoke(this, System.EventArgs.Empty);
            });
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        this.AppWindow.Hide();
    }
}
