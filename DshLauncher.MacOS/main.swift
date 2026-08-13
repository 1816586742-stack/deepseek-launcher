import Cocoa
import WebKit

class AppDelegate: NSObject, NSApplicationDelegate {
    var window: NSWindow!
    var webView: WKWebView!
    let defaultUrl = "http://127.0.0.1:3080"

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Single instance check via distributed notification center
        let running = NSWorkspace.shared.runningApplications.filter {
            $0.bundleIdentifier == Bundle.main.bundleIdentifier
        }
        if running.count > 1 {
            NSApp.terminate(nil)
            return
        }

        // Auto-start dsh if not running
        if !portOpen(port: 3080) {
            startDsh()
            // Wait up to 90 seconds
            for _ in 0..<90 {
                Thread.sleep(forTimeInterval: 1)
                if portOpen(port: 3080) { break }
            }
        }

        guard portOpen(port: 3080) else {
            let alert = NSAlert()
            alert.messageText = "dsh service not available"
            alert.informativeText = "Check: ~/.dsh"
            alert.runModal()
            NSApp.terminate(nil)
            return
        }

        // Create window
        window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1280, height: 840),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )
        window.title = "DeepSeek Harness"
        window.minSize = NSSize(width: 800, height: 600)
        window.center()

        // Create webview
        let config = WKWebViewConfiguration()
        webView = WKWebView(frame: window.contentView!.bounds, configuration: config)
        webView.autoresizingMask = [.width, .height]
        window.contentView!.addSubview(webView)

        // Open external links in Safari
        webView.navigationDelegate = self

        window.makeKeyAndOrderFront(nil)
        webView.load(URLRequest(url: URL(string: defaultUrl)!))
    }

    func startDsh() {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: "/bin/sh")
        task.arguments = ["-c", "npx -y @deepseek-ai/dsh web"]
        try? task.run()
    }

    func portOpen(port: Int) -> Bool {
        var socket = sockaddr_in()
        socket.sin_family = sa_family_t(AF_INET)
        socket.sin_port = UInt16(port).bigEndian
        socket.sin_addr.s_addr = inet_addr("127.0.0.1")
        let fd = socket(AF_INET, SOCK_STREAM, 0)
        defer { close(fd) }
        return connect(fd, sockaddr_cast(&socket), socklen_t(MemoryLayout<sockaddr_in>.size)) == 0
    }

    func sockaddr_cast(_ addr: inout sockaddr_in) -> UnsafePointer<sockaddr> {
        return withUnsafePointer(to: &addr) { UnsafeRawPointer($0).assumingMemoryBound(to: sockaddr.self) }
    }
}

extension AppDelegate: WKNavigationDelegate {
    func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction, decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        if let url = navigationAction.request.url, !url.host!.contains("127.0.0.1") {
            NSWorkspace.shared.open(url)
            decisionHandler(.cancel)
        } else {
            decisionHandler(.allow)
        }
    }
}

// Entry point
let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.run()
