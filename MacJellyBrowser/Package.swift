// swift-tools-version:5.7
import PackageDescription

let package = Package(
    name: "MacJellyBrowser",
    platforms: [
        .macOS(.v11)
    ],
    products: [
        .executable(name: "MacJellyBrowser", targets: ["MacJellyBrowser"])
    ],
    targets: [
        .executableTarget(
            name: "MacJellyBrowser",
            dependencies: []
        )
    ]
)
