using System.Runtime.InteropServices;
using ObjCRuntime;

namespace example;

public static partial class NativeCrashHelper
{
    [DllImport("__Internal")]
    private static extern void abort();

    public static partial void TriggerNativeCrash()
    {
        // Call C-level abort() which generates a SIGABRT - a true native crash
        abort();
    }
}
