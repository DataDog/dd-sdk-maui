namespace Datadog.Maui;

/// <summary>
/// Provides crash reporting functionality for the Datadog SDK.
/// </summary>
/// <remarks>
/// This class enables both native crash reporting (iOS/Android) and .NET managed exception handling.
/// Native crash reporting captures crashes at the OS level, while managed exception handling
/// catches unhandled .NET exceptions and reports them to RUM.
/// </remarks>
public static partial class CrashReporting
{
    /// <summary>
    /// Enables native crash reporting on the current platform.
    /// Must be called after the Datadog SDK is initialized.
    /// </summary>
    /// <returns>True if native crash reporting was enabled successfully, false otherwise.</returns>
    /// <remarks>
    /// On iOS, this enables the DatadogCrashReporting module.
    /// On Android, this enables the NDK crash reporter for native crashes.
    /// </remarks>
    public static partial bool EnableNative();

    /// <summary>
    /// Enables managed exception handling for .NET unhandled exceptions.
    /// </summary>
    /// <param name="forwardToRum">
    /// If true, unhandled exceptions will be reported to RUM as errors.
    /// Default is true.
    /// </param>
    /// <remarks>
    /// This method hooks into AppDomain.CurrentDomain.UnhandledException
    /// and TaskScheduler.UnobservedTaskException to capture
    /// unhandled exceptions from the .NET runtime.
    /// </remarks>
    public static void EnableManagedExceptionHandling(bool forwardToRum = true)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex && forwardToRum)
            {
                try
                {
                    Rum.Instance.AddError(
                        ex.Message,
                        RumErrorSource.Source,
                        ex.StackTrace,
                        new Dictionary<string, object>
                        {
                            ["error.kind"] = "UnhandledException",
                            ["error.type"] = ex.GetType().FullName ?? ex.GetType().Name,
                            ["error.is_terminating"] = args.IsTerminating
                        });
                }
                catch
                {
                    // Silently fail if RUM is not available
                }
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            if (forwardToRum)
            {
                try
                {
                    var baseException = args.Exception.GetBaseException();
                    Rum.Instance.AddError(
                        baseException.Message,
                        RumErrorSource.Source,
                        args.Exception.StackTrace,
                        new Dictionary<string, object>
                        {
                            ["error.kind"] = "UnobservedTaskException",
                            ["error.type"] = baseException.GetType().FullName ?? baseException.GetType().Name
                        });
                }
                catch
                {
                    // Silently fail if RUM is not available
                }
            }
        };
    }
}
