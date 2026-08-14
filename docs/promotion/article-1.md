# DeepSeek Launcher: 642KB 的全平台桌面启动器

> 告别 Electron 的臃肿,用原生技术打造轻量级 AI 桌面工具

---

## 你是否遇到过这些问题?

使用 DeepSeek Harness (dsh) 时,每次都要:

1. 打开终端,输入 `npx @deepseek-ai/dsh web`
2. 等待服务启动,手动复制地址到浏览器
3. 关掉终端,dsh 就停了
4. 想在手机上用?抱歉,只有网页版

**是时候改变这一切了。**

## DeepSeek Launcher 是什么?

一个**轻量级、全平台**的 DeepSeek Harness 桌面启动器:

| 特性 | 说明 |
|---|---|
| 🚀 **极轻量** | 仅 642KB(Electron 方案需要 135MB) |
| 🌍 **全平台** | Windows / macOS / Linux |
| 🐳 **一键启动** | 双击即用,自动拉起 dsh |
| 🔄 **自动更新** | 启动时检查新版本 |
| ⚙️ **配置简单** | 右键即可设置 |

## 技术架构:为什么这么轻?

### Electron 的问题

大多数桌面应用使用 Electron,它会打包完整的 Chromium 浏览器 + Node.js 运行时,导致:

- 安装包 100-200MB
- 内存占用 200-500MB
- 启动慢(加载 Chromium)

### 我们的方案:原生 WebView

```
Electron 方案:
┌─────────────────────────────┐
│  Chromium (100MB)           │
│  Node.js (50MB)             │
│  你的应用代码 (1MB)          │
└─────────────────────────────┘

DeepSeek Launcher:
┌─────────────────────────────┐
│  系统 WebView2/WebKit (0MB)  │  ← Windows 11 内置
│  你的应用代码 (642KB)        │
└─────────────────────────────┘
```

每个平台使用**系统自带的 WebView**:
- **Windows**: WebView2(Windows 11 内置,Win10 可装)
- **macOS**: WKWebView(Safari 内核)
- **Linux**: WebKitGTK

**结果**:安装包从 135MB 缩小到 642KB,**体积减少 99.5%**。

## 核心功能

### 1. 一键启动

双击应用 → 自动检测 dsh 是否运行 → 没运行就自动拉起 → 加载 Web UI

```
用户操作: 双击图标
系统行为: 检测端口 → npx dsh web → 等待就绪 → 打开窗口
用户看到: DeepSeek Harness 界面
```

### 2. 智能链接

- 点击外部链接 → 自动在系统浏览器打开
- 点击内部链接 → 在应用内打开

### 3. 持久运行

- 关闭窗口 → 应用驻留托盘
- 后台 dsh 进程继续运行
- 从托盘菜单可以重新打开或退出

### 4. 配置灵活

右键托盘图标 → 设置:
- dsh 端口(默认 3080)
- 开机自启动
- 语言切换

## 安装方式

### 方式一:脚本版(开发者,0 KB)

如果你已经有 Node.js:

**Windows**: 双击 `start-dsh.bat`

**macOS/Linux**:
```bash
chmod +x start-dsh.sh
./start-dsh.sh
```

### 方式二:桌面版(普通用户)

从 [GitHub Releases](https://github.com/1816586742-stack/deepseek-launcher/releases) 下载:

| 平台 | 文件 |
|---|---|
| Windows x64 | `dsh-launcher_win_x64.zip` |
| Windows arm64 | `dsh-launcher_win_arm64.zip` |
| macOS | `dsh-launcher_macos.tar.gz` |
| Linux | `dsh-launcher_linux.tar.gz` |

解压后双击运行即可。

## 技术栈

| 平台 | 语言 | WebView | 代码量 |
|---|---|---|---|
| Windows | C# | WebView2 | ~666 行 |
| macOS | Swift | WKWebView | ~70 行 |
| Linux | Python | WebKitGTK | ~80 行 |

**三平台合计仅 816 行代码**,实现了完整的桌面体验。

## 与 Electron 方案对比

| 指标 | Electron | DeepSeek Launcher |
|---|---|---|
| 安装包大小 | 100-200 MB | **642 KB** |
| 内存占用 | 200-500 MB | **50-100 MB** |
| 启动速度 | 3-5 秒 | **<1 秒** |
| dsh 版本 | 固定打包 | **npx 永远最新** |
| 插件兼容 | ❌ 受限 | ✅ 完全兼容 |

## 为什么选择原生而不是 Electron?

1. **轻量**: 用户不需要下载 135MB 的 Chromium
2. **快速**: 系统 WebView 已经加载,启动几乎瞬时
3. **兼容**: dsh 的插件系统完全可用(通过 npx)
4. **维护**: 代码量少(816 行),易于维护

## 开源地址

- **GitHub**: https://github.com/1816586742-stack/deepseek-launcher
- **许可证**: MIT
- **欢迎 Star 和 PR!**

## 写在最后

DeepSeek Harness 是一个强大的 AI agent 框架,但它的桌面体验可以更好。

DeepSeek Launcher 的目标很简单:**让 dsh 的使用体验像打开一个本地应用一样简单**。

如果你也在使用 DeepSeek Harness,欢迎试用 DeepSeek Launcher,给我们一个 Star ⭐

---

*作者:盗天是也*
*日期:2026年8月14日*
