using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Threading;
using System.Threading.Tasks;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;

namespace PicPicker;

// https://johngagefaulkner.github.io/01-Build-a-Single-Instance-WinUI-3-App-2026-Gemini-2.5-Pro.html

public static class Program
{
    // Define a unique key for your app instance
    public const string AppInstanceKey = "get-picpicker";

    [STAThread] // Must be single-threaded
    static void Main(string[] args)
    {
        // Initialize WinRT (required)
        WinRT.ComWrappersSupport.InitializeComWrappers();

        // Check if we are being activated by a protocol
        AppActivationArguments activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // 1. Find or register the main instance
        AppInstance mainInstance = AppInstance.FindOrRegisterForKey(AppInstanceKey);

        // 2. If this is the main instance...
        if (mainInstance.IsCurrent)
        {
            // ...register to handle future activations (from other instances)
            mainInstance.Activated += OnAppActivated;

            // Run the app's OnLaunched code
            // We pass the args here, but OnLaunched will also get them.
            // This is just to ensure the first launch works correctly.
            StartApp(activationArgs);
        }
        // 3. If this is NOT the main instance...
        else
        {
            // ...redirect the activation to the main instance and exit.
            // This sends our activationArgs (e.g., the protocol URI) to the
            // 'OnAppActivated' handler in the main instance.
            Task.WaitAll(mainInstance.RedirectActivationToAsync(activationArgs).AsTask());
            Environment.Exit(0);
        }
    }

    private static void StartApp(AppActivationArguments args)
    {
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    // This handler runs in the *main instance* when a *new instance* redirects to it
    private static void OnAppActivated(object? sender, AppActivationArguments args)
    {
        // This event is NOT on the UI thread.
        // We must use the DispatcherQueue to safely update the UI.

        if (Application.Current is App app)
        {
            app.OnAppActivated();
        }
    }
}