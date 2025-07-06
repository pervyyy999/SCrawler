import SwiftUI

@main
struct JellyBrowserApp: App {
    @State private var address: String = "https://www.google.com"

    var body: some Scene {
        WindowGroup {
            TabView {
                WebView(url: URL(string: "http://localhost:8096")!)
                    .tabItem { Text("Jellyfin") }
                WebView(url: URL(string: address)!)
                    .tabItem { Text("Browser") }
            }
        }
    }
}
