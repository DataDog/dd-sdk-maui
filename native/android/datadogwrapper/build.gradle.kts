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
    // Embed these dependencies in the fat AAR
    api("com.datadoghq:dd-sdk-android-rum:2.22.0")
    api("com.datadoghq:dd-sdk-android-logs:2.22.0")
    api("com.datadoghq:dd-sdk-android-ndk:2.22.0")
}

// Create fat AAR that bundles ONLY Datadog SDK core dependencies
afterEvaluate {
    tasks.named("bundleReleaseAar").configure {
        doLast {
            val aarFile = file("build/outputs/aar/datadogwrapper-release.aar")
            val fatAarFile = file("build/outputs/aar/datadogwrapper-fat-release.aar")

            // Only include Datadog SDK modules (whitelist approach)
            val includePatterns = listOf(
                "dd-sdk-android",    // Datadog SDK modules
                "kronos"             // Clock library used by Datadog
            )

            fun shouldInclude(fileName: String): Boolean {
                return includePatterns.any { pattern ->
                    fileName.lowercase().contains(pattern.lowercase())
                }
            }

            if (aarFile.exists()) {
                // Collect all dependency JARs
                val runtimeClasspath = configurations.getByName("releaseRuntimeClasspath")
                val depJars = runtimeClasspath.filter {
                    (it.name.endsWith(".jar") || it.name.endsWith(".aar")) && shouldInclude(it.name)
                }

                // Create fat AAR by merging classes
                val tempDir = file("build/fat-aar-temp")
                tempDir.deleteRecursively()
                tempDir.mkdirs()

                // Extract original AAR
                copy {
                    from(zipTree(aarFile))
                    into(tempDir)
                }

                // Extract classes from dependency AARs and JARs
                val libsDir = File(tempDir, "libs")
                libsDir.mkdirs()

                var includedCount = 0
                var excludedCount = 0

                runtimeClasspath.forEach { dep ->
                    if (dep.name.endsWith(".jar") || dep.name.endsWith(".aar")) {
                        if (!shouldInclude(dep.name)) {
                            excludedCount++
                            println("  Excluding: ${dep.name}")
                        } else {
                            includedCount++
                            if (dep.name.endsWith(".aar")) {
                                // Extract classes.jar from AAR and add to libs
                                val aarTemp = file("build/aar-extract-${dep.nameWithoutExtension}")
                                aarTemp.deleteRecursively()
                                copy {
                                    from(zipTree(dep))
                                    into(aarTemp)
                                }
                                val classesJar = File(aarTemp, "classes.jar")
                                if (classesJar.exists()) {
                                    copy {
                                        from(classesJar)
                                        into(libsDir)
                                        rename { "${dep.nameWithoutExtension}.jar" }
                                    }
                                }
                                // Also copy any libs/*.jar from the AAR
                                val aarLibs = File(aarTemp, "libs")
                                if (aarLibs.exists()) {
                                    copy {
                                        from(aarLibs)
                                        into(libsDir)
                                    }
                                }
                            } else if (dep.name.endsWith(".jar")) {
                                copy {
                                    from(dep)
                                    into(libsDir)
                                }
                            }
                            println("  Including: ${dep.name}")
                        }
                    }
                }

                // Create fat AAR
                ant.withGroovyBuilder {
                    "zip"("destfile" to fatAarFile, "basedir" to tempDir)
                }

                // Replace original with fat version
                fatAarFile.copyTo(aarFile, overwrite = true)

                println("Fat AAR: included $includedCount deps, excluded $excludedCount deps")
                println("Fat AAR size: ${aarFile.length() / 1024} KB")
            }
        }
    }
}
