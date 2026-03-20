# Auto-generated file - Do not edit

# Keep all Datadog SDK classes and interfaces
-keep class com.datadog.** { *; }
-keep interface com.datadog.** { *; }

# Keep Kotlin metadata (required for Kotlin stdlib)
-keep class kotlin.Metadata { *; }

# Keep methods with @JvmStatic annotation
-keepclassmembers class * {
    @kotlin.jvm.JvmStatic *;
}

# --- Rules from dd-sdk-android-logs-3.8.0.aar ---
# This is needed for the Datadog Error Tracking feature to work reliably,
 # this file is used by Logs and RUM modules
-keepattributes SourceFile,LineNumberTable
