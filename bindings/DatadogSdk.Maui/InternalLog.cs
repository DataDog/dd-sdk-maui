namespace DatadogSdk.Maui
{
    public enum SdkVerbosity
    {
        DEBUG = 0,
        INFO = 1,
        WARN = 2,
        ERROR = 3
    }

    /// <summary>
    /// Internal logger for debugging the Datadog SDK.
    /// DO NOT USE THIS IN YOUR APP — this is for SDK diagnostics only.
    /// </summary>
    internal static class InternalLog
    {
        private const string Prefix = "DATADOG:";

        internal static SdkVerbosity? Verbosity { get; set; } = null;

        internal static void Log(string message, SdkVerbosity level)
        {
            if (Verbosity == null)
                return;

            if ((int)level >= (int)Verbosity.Value)
            {
                Console.WriteLine($"{Prefix} [{level}] {message}");
            }
        }
    }
}
