import Cocoa
import WebKit

class AppDelegate: NSObject, NSApplicationDelegate {
    var window: NSWindow!
    var webView: WKWebView!
    let port: Int
    
    override init() {
        // Read port from config file, default 3080
        let configPath = NSHomeDirectory() + "/.dsh-launcher/config.json"
        var p = 3080
        if let data = FileManager.default.contents(atPath: configPath),
           let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
           let configPort = json["port"] as? Int {
            p = configPort
        }
        port = p
        super.init()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Auto-start dsh if not running
        if !portOpen(port: port) {
            startDsh()
            Thread.sleep(forTimeInterval: 5)
        }

        // Create window
        window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1280, height: 840),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )
        window.title = "DeepSeek Harness"
        window.center()

        // Create webview
        webView = WKWebView(frame: window.contentView!.bounds)
        webView.autoresizingMask = [.width, .height]
        window.contentView!.addSubview(webView)
        webView.navigationDelegate = self

        // Set app icon
        if let iconPath = Bundle.main.path(forResource: "icon", ofType: "png"),
           let image = NSImage(contentsOfFile: iconPath) {
            NSApp.applicationIconImage = image
        }

        window.makeKeyAndOrderFront(nil)
        webView.load(URLRequest(url: URL(string: "http://127.0.0.1:\(port)")!))
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return true
    }

    func startDsh() {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: "/bin/sh")
        task.arguments = ["-c", "npx -y @deepseek-ai/dsh web"]
        try? task.run()
    }

    func portOpen(port: Int) -> Bool {
        var addr = sockaddr_in()
        addr.sin_family = sa_family_t(AF_INET)
        addr.sin_port = UInt16(port).bigEndian
        addr.sin_addr.s_addr = inet_addr("127.0.0.1")
        let sock = socket(AF_INET, SOCK_STREAM, 0)
        defer { close(sock) }
        return withUnsafePointer(to: &addr) {
            connect(sock, UnsafeRawPointer($0).assumingMemoryBound(to: sockaddr.self), socklen_t(MemoryLayout<sockaddr_in>.size))
        } == 0
    }
}

extension AppDelegate: WKNavigationDelegate {
    func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction, decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        if let url = navigationAction.request.url, let host = url.host, !host.contains("127.0.0.1") {
            NSWorkspace.shared.open(url)
            decisionHandler(.cancel)
        } else {
            decisionHandler(.allow)
        }
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
