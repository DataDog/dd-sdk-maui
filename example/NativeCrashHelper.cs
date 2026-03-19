namespace example;

public static partial class NativeCrashHelper
{
    public static partial void TriggerNativeCrash();
#if ANDROID
    public static partial void TriggerNdkCrash();
#endif
}
