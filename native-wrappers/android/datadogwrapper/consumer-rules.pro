# Keep all Datadog SDK classes and interfaces
-keep class com.datadog.** { *; }
-keep interface com.datadog.** { *; }

# Keep Kotlin metadata (required for Kotlin stdlib)
-keep class kotlin.Metadata { *; }

# Keep methods with @JvmStatic annotation
-keepclassmembers class * {
    @kotlin.jvm.JvmStatic *;
}
