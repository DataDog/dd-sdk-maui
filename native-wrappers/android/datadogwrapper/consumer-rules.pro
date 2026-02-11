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

# Prevent obfuscation of wrapper APIs
-keepnames class com.datadog.wrapper.DatadogWrapper
-keepnames class com.datadog.wrapper.DdLogs
