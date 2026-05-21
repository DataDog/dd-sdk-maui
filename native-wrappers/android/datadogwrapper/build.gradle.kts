plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.datadog.wrapper"
    compileSdk = 34

    defaultConfig {
        minSdk = 23
        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }

    kotlinOptions {
        jvmTarget = "1.8"
    }
}

dependencies {
    // Use 'implementation' to hide dependencies from AAR consumers
    // Dependencies will be needed at runtime but won't be in the binding API
    implementation("com.datadoghq:dd-sdk-android-core:3.10.0")
    implementation("com.datadoghq:dd-sdk-android-logs:3.10.0")
    implementation("com.datadoghq:dd-sdk-android-trace:3.10.0")
    implementation("com.datadoghq:dd-sdk-android-rum:3.10.0")
    implementation("com.datadoghq:dd-sdk-android-ndk:3.10.0")
    implementation("com.datadoghq:dd-sdk-android-session-replay:3.10.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")

    // Test dependencies
    testImplementation("junit:junit:4.13.2")
    testImplementation("io.mockk:mockk:1.13.16")
}
