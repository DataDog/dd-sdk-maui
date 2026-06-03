// swift-tools-version:5.9
/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */
import PackageDescription

let package = Package(
    name: "DatadogWrapper",
    platforms: [
        .iOS(.v12),
        // macOS is required to run unit tests locally via `swift test`.
        .macOS("12.6")
    ],
    products: [
        .library(
            name: "DatadogWrapper",
            type: .dynamic,
            targets: ["DatadogWrapper"])
    ],
    dependencies: [
        .package(url: "https://github.com/DataDog/dd-sdk-ios.git", from: "3.11.0")
    ],
    targets: [
        .target(
            name: "DatadogWrapper",
            dependencies: [
                .product(name: "DatadogCore", package: "dd-sdk-ios"),
                .product(name: "DatadogLogs", package: "dd-sdk-ios"),
                .product(name: "DatadogTrace", package: "dd-sdk-ios"),
                .product(name: "DatadogRUM", package: "dd-sdk-ios"),
                .product(name: "DatadogCrashReporting", package: "dd-sdk-ios"),
                .product(name: "DatadogSessionReplay", package: "dd-sdk-ios")
            ],
            path: "Sources/DatadogWrapper"
        ),
        .testTarget(
            name: "DatadogWrapperTests",
            dependencies: ["DatadogWrapper"],
            path: "Tests/DatadogWrapperTests"
        )
    ]
)
