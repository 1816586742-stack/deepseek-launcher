#!/usr/bin/env python3
"""DSH Launcher — Linux (GTK4 + WebKitGTK)"""

import subprocess, socket, time, sys, os, json
import gi
gi.require_version('Gtk', '4.0')
gi.require_version('WebKit', '6.0')
from gi.repository import Gtk, WebKit, GLib

DEFAULT_PORT = 3080

def load_port() -> int:
    """Read port from config file, default 3080."""
    config_path = os.path.expanduser("~/.dsh-launcher/config.json")
    try:
        with open(config_path) as f:
            cfg = json.load(f)
            return cfg.get("port", DEFAULT_PORT)
    except:
        return DEFAULT_PORT

def port_open(port: int) -> bool:
    try:
        with socket.create_connection(("127.0.0.1", port), timeout=1):
            return True
    except:
        return False

def start_dsh():
    log_path = os.path.expanduser("~/.dsh-web.log")
    with open(log_path, "a") as log:
        subprocess.Popen(
            ["npx", "-y", "@deepseek-ai/dsh", "web"],
            stdout=log,
            stderr=subprocess.STDOUT,
        )

def main():
    port = load_port()

    # Single instance via lock file
    lock = os.path.expanduser("~/.dsh-launcher.lock")
    try:
        fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        os.write(fd, str(os.getpid()).encode())
        os.close(fd)
    except FileExistsError:
        print("Already running")
        sys.exit(0)

    import atexit
    def cleanup():
        try: os.unlink(lock)
        except: pass
    atexit.register(cleanup)

    # Auto-start dsh
    if not port_open(port):
        start_dsh()
        for _ in range(90):
            time.sleep(1)
            if port_open(port):
                break

    if not port_open(port):
        print(f"dsh service not available on port {port}. Check: ~/.dsh-web.log")
        sys.exit(1)

    # Create window
    win = Gtk.Window(title="DeepSeek Harness")
    win.set_default_size(1280, 840)
    win.set_size_request(800, 600)
    win.connect("destroy", lambda _: sys.exit(0))

    # WebKit webview
    web = WebKit.WebView()
    settings = web.get_settings()
    settings.set_enable_javascript(True)
    web.load_uri(f"http://127.0.0.1:{port}")

    # Open external links in default browser
    def on_decide_policy(webview, decision, type_):
        if type_ == WebKit.PolicyDecisionType.NAVIGATION_ACTION:
            nav = decision.get_navigation_action()
            req = nav.get_request()
            uri = req.get_uri() if req else ""
            if uri and "127.0.0.1" not in uri:
                import webbrowser
                webbrowser.open(uri)
                decision.ignore()
                return True
        return False

    web.connect("decide-policy", on_decide_policy)
    win.set_child(web)
    win.present()

    Gtk.main()

if __name__ == "__main__":
    main()
