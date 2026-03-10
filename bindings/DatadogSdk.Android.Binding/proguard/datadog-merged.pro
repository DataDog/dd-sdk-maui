# Keep all Datadog SDK classes
-keep class com.datadog.** { *; }
-keep interface com.datadog.** { *; }

# Keep wrapper classes and all their methods
-keep class com.datadog.wrapper.** { *; }

# Keep Kotlin metadata (required for Kotlin stdlib)
-keep class kotlin.Metadata { *; }

# Keep methods with @JvmStatic annotation
-keepclassmembers class * {
    @kotlin.jvm.JvmStatic *;
}

# Kotlin companion objects — required for @JvmStatic dispatch from C# via JNI
-keep class com.datadog.wrapper.DatadogWrapper$Companion { *; }
-keep class com.datadog.wrapper.DdLogs$Companion { *; }

# Prevent obfuscation of wrapper APIs
-keepnames class com.datadog.wrapper.DatadogWrapper
-keepnames class com.datadog.wrapper.DdLogs

# --- Rules from dd-sdk-android-logs-3.5.0.aar ---
# This is needed for the Datadog Error Tracking feature to work reliably,
 # this file is used by Logs and RUM modules
-keepattributes SourceFile,LineNumberTable
