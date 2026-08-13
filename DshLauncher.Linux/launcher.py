#!/usr/bin/env python3
"""DSH Launcher — Linux (GTK4 + WebKitGTK)"""

import subprocess, socket, time, sys, os
import gi
gi.require_version('Gtk', '4.0')
gi.require_version('WebKit', '6.0')
from gi.repository import Gtk, WebKit, GLib

DEFAULT_URL = "http://127.0.0.1:3080"

def port_open(port: int) -> bool:
    try:
        with socket.create_connection(("127.0.0.1", port), timeout=1):
            return True
    except:
        return False

def start_dsh():
    subprocess.Popen(
        ["npx", "-y", "@deepseek-ai/dsh", "web"],
        stdout=open(os.path.expanduser("~/.dsh-web.log"), "a"),
        stderr=subprocess.STDOUT,
    )

def main():
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
    if not port_open(3080):
        start_dsh()
        for _ in range(90):
            time.sleep(1)
            if port_open(3080):
                break

    if not port_open(3080):
        print("dsh service not available. Check: ~/.dsh-web.log")
        sys.exit(1)

    # Create window
    win = Gtk.Window(title="DeepSeek Harness")
    win.set_default_size(1280, 840)
    win.set_size_request(800, 600)

    # WebKit webview
    web = WebKit.WebView()
    settings = web.get_settings()
    settings.set_enable_javascript(True)
    settings.set_allow_file_access_from_file_urls(False)

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
    web.load_uri(DEFAULT_URL)

    # Set window icon
    icon_path = os.path.join(os.path.dirname(__file__), "..", "..", "assets", "icon.jpg")
    if os.path.exists(icon_path):
        win.set_icon_name(icon_path)

    win.set_child(web)
    win.connect("destroy", lambda _: sys.exit(0))
    win.present()

    Gtk.main()

if __name__ == "__main__":
    main()
