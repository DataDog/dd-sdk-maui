plugins {
    id("com.android.library")
    kotlin("android")
}

android {
    namespace = "com.datadog.maui"
    compileSdk = 35

    defaultConfig {
        minSdk = 26
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
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation("com.datadoghq:dd-sdk-android-rum:2.22.0")
    implementation("com.datadoghq:dd-sdk-android-logs:2.22.0")
}
