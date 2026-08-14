# DSH Launcher

[English](README.md) | 中文

> 轻量级 DeepSeek Harness 全平台启动器 — 脚本版 0 KB,桌面版 642 KB

![License](https://img.shields.io/badge/License-MIT-blue)
![Version](https://img.shields.io/badge/Version-v0.3.4-green)

## 特性

- 🚀 **极轻量**: 脚本版 0 KB,桌面版仅 642 KB
- 🌍 **全平台**: Windows / macOS / Linux
- 🐳 **按需拉取 dsh**: 通过 `npx` 自动获取最新版本,插件完全兼容
- 🔄 **更新检查**: 启动时自动检查 GitHub Releases(可跳过版本),或在设置中手动检查
- ⚙️ **设置面板**: 端口配置 / 开机自启 / 语言切换
- 🐳 **DeepSeek 鲸鱼图标**: 官方 logo 嵌入
- 📋 **专业更新对话框**: 版本对比 + 分类更新日志(参考 Bili23 设计)

## 快速开始

### 脚本版(开发者,0 KB)

需要 Node.js 18+:

**Windows**: 双击 `scripts/start-dsh.bat`

**macOS/Linux**:
```bash
chmod +x scripts/start-dsh.sh
./scripts/start-dsh.sh
```

### 桌面版(普通用户)

从 [Releases](https://github.com/1816586742-stack/dsh-launcher-cross/releases) 下载:

| 文件 | 平台 | 说明 |
|---|---|---|
| `dsh-launcher_win_x64.zip` | Windows | 解压运行 DshLauncher.Windows.exe |
| `dsh-launcher_macos.tar.gz` | macOS | 解压后编译或直接运行脚本 |
| `dsh-launcher_linux.tar.gz` | Linux | 解压后运行 launcher.py |

### 从源码编译

**Windows** (.NET SDK 10 + WebView2):
```bash
cd DshLauncher.Windows
dotnet run
```

**macOS** (Swift + WebKit):
```bash
cd DshLauncher.MacOS
swiftc main.swift -o dsh-launcher -framework Cocoa -framework WebKit
./dsh-launcher
```

**Linux** (Python + GTK4):
```bash
cd DshLauncher.Linux
pip install pygobject
python3 launcher.py
```
需要: `sudo apt install libgtk-4-dev libwebkitgtk-6.0-dev`

## 功能

- 🚀 **自动拉起 dsh**: 检测端口,没开就 `npx -y @deepseek-ai/dsh web`
- 🔄 **手动更新**: 设置面板 → Check for updates(不自动打扰)
- ⚙️ **设置面板**: 右键 → Settings(端口/auto-start)
- 🐳 **DeepSeek 鲸鱼图标**: 官方 logo
- 🔗 **智能链接**: 外部链接走系统浏览器

## 更新方式

DSH Launcher 不会自动检查更新。手动更新:
1. 打开设置面板(右键 → Settings)
2. 点击 "Check for updates"
3. 有新版本时会提示打开下载页面

## 工作原理

```
启动 → 检测端口 3080 → 没开? → npx -y @deepseek-ai/dsh web
                         ↓
                    等待就绪(最多90秒)
                         ↓
                    系统 WebView → http://127.0.0.1:3080
```

## 架构决策

详见 [docs/adr/001-architecture-decisions.md](docs/adr/001-architecture-decisions.md)

## 贡献

欢迎贡献!详见 [CONTRIBUTING.md](CONTRIBUTING.md)

## 免责声明

本项目是独立的第三方工具,与 DeepSeek / DeepSeek AI 官方无关。
应用图标使用了 DeepSeek 品牌标识,版权归 DeepSeek 所有,仅作个人本地使用。

## 许可证

[MIT](LICENSE)
