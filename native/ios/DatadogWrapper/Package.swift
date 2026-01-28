// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "DatadogWrapper",
    platforms: [.iOS(.v17)],
    products: [
        .library(name: "DatadogWrapper", type: .static, targets: ["DatadogWrapper"])
    ],
    dependencies: [
        .package(url: "https://github.com/DataDog/dd-sdk-ios.git", exact: "2.22.0")
    ],
    targets: [
        .target(
            name: "DatadogWrapper",
            dependencies: [
                .product(name: "DatadogCore", package: "dd-sdk-ios")
            ]
        )
    ]
)
