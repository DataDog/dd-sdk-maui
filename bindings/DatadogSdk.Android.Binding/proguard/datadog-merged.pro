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

# --- Rules from dd-sdk-android-logs-3.9.0.aar ---
# This is needed for the Datadog Error Tracking feature to work reliably,
 # this file is used by Logs and RUM modules
-keepattributes SourceFile,LineNumberTable

# --- Rules from dd-sdk-android-trace-3.9.0.aar ---
-keep class com.datadog.android.trace.GlobalDatadogTracer {
    public com.datadog.android.trace.api.tracer.DatadogTracer getOrNull();
    public static com.datadog.android.trace.GlobalDatadogTracer INSTANCE;
}
-keepclassmembernames class org.jctools.** { *; }

# --- Rules from dd-sdk-android-rum-3.9.0.aar ---
# This is needed for the Datadog Error Tracking feature to work reliably,
 # this file is used by Logs and RUM modules
-keepattributes SourceFile,LineNumberTable

# Kept for our internal telemetry
-keepnames class com.datadog.android.rum.internal.monitor.DatadogRumMonitor
-keepnames class com.datadog.android.rum.internal.domain.scope.RumRawEvent
-keepnames class * extends com.datadog.android.rum.internal.domain.scope.RumRawEvent

# --- Rules from dd-sdk-android-session-replay-3.9.0.aar ---
# Keep the optional selector class name. We need this in the SR recorder.
-keepnames class * extends android.view.View
-keepnames class * extends android.graphics.drawable.Drawable
-keepnames class * extends android.graphics.ColorFilter

# Kept for our internal telemetry
-keepnames class com.datadog.android.sessionreplay.internal.recorder.listener.WindowsOnDrawListener
-keepnames class com.datadog.android.sessionreplay.internal.recorder.TreeViewTraversal
-keepnames class * extends com.datadog.android.sessionreplay.recorder.mapper.WireframeMapper
-keepnames class * extends com.datadog.android.sessionreplay.internal.async.RecordedDataQueueItem

# Keep the fine grained masking level enums
-keepnames enum * extends com.datadog.android.sessionreplay.PrivacyLevel { *; }
