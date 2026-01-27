// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "DatadogWrapper",
    platforms: [
        .iOS(.v12)
    ],
    products: [
        .library(
            name: "DatadogWrapper",
            type: .dynamic,
            targets: ["DatadogWrapper"])
    ],
    dependencies: [
        .package(url: "https://github.com/DataDog/dd-sdk-ios.git", from: "3.5.0")
    ],
    targets: [
        .target(
            name: "DatadogWrapper",
            dependencies: [
                .product(name: "DatadogCore", package: "dd-sdk-ios"),
                .product(name: "DatadogLogs", package: "dd-sdk-ios")
            ],
            path: "Sources/DatadogWrapper"
        )
    ]
)
