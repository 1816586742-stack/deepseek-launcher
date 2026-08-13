# DSH Launcher

轻量级 DeepSeek Harness 全平台启动器。

## 版本

| 版本 | 平台 | 体积 | 依赖 |
|---|---|---|---|
| 脚本版 | Win/Mac/Linux | 0 KB | Node.js |
| Windows 桌面版 | Windows | ~150 行 C# | .NET + WebView2 |
| macOS 桌面版 | macOS | ~120 行 Swift | Swift + WKWebView |
| Linux 桌面版 | Linux | ~100 行 Python | Python + GTK4 + WebKit |

## 快速开始(脚本版)

### Windows
双击 `scripts/start-dsh.bat`

### macOS / Linux
```bash
chmod +x scripts/start-dsh.sh
./scripts/start-dsh.sh
```

## 桌面版(Windows)

```bash
cd DshLauncher.Windows
dotnet run
```

需要 .NET SDK 10 + WebView2 Runtime(Windows 11 内置)。

## 桌面版(macOS)

```bash
cd DshLauncher.MacOS
swiftc main.swift -o dsh-launcher -framework Cocoa -framework WebKit
./dsh-launcher
```

## 桌面版(Linux)

```bash
cd DshLauncher.Linux
pip install pygobject
python3 launcher.py
```

需要 GTK4 + WebKitGTK(`sudo apt install libgtk-4-dev libwebkitgtk-6.0-dev`)。

## 工作原理

1. 检测 dsh 端口(默认 3080)是否已开放
2. 没开 → 通过 `npx -y @deepseek-ai/dsh web` 自动拉起
3. 等待服务就绪(最多 90 秒)
4. 打开系统 WebView 加载 `http://127.0.0.1:3080`
5. 外部链接 → 系统默认浏览器

## 免责声明

本项目是独立的第三方工具,与 DeepSeek / DeepSeek AI 官方无关。
应用图标使用了 DeepSeek 品牌标识,版权归 DeepSeek 所有,仅作个人本地使用。

## 许可证

MIT
