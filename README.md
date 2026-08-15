# DeepSeek Launcher

[English](README.en.md) | 中文

> 轻量级 C# WebView2 壳启动器 — 对标 DSH Desktop 全部核心功能

![License](https://img.shields.io/badge/License-MIT-blue)
![Version](https://img.shields.io/badge/Version-v0.3.7-green)
![Tests](https://img.shields.io/badge/Tests-87%20passed-brightgreen)

## 功能

- 🚀 **极轻量**: 单文件 ~1.7 MB,无 Electron,无 Node 打包
- 🐳 **自动拉起 dsh**: 端口未就绪时自动启动 dsh 服务(复用 npx)
- 🔄 **服务看门狗**: 每 5s 探测端口,服务断开自动重启 + 页面重载
- 📢 **会话完成通知**: 增量监视 zstd 会话日志,agent 轮次结束弹托盘通知
- 💰 **余额查询**: 右键菜单一键查 DeepSeek 账户余额
- 📥 **智能下载**: Content-Disposition 解析 + MIME 扩展名补全 + 安全扩展白名单自动打开
- 🪟 **弹窗分类**: 外部链接→系统浏览器,同源弹窗→壳内窗口(保留会话)
- 🎨 **启动动画**: 鲸鱼 logo + loading + "正在启动..." 等待页
- 📋 **完整右键菜单**: Reload / DevTools / Fullscreen / Open in Browser / Open Log Dir
- 🔒 **最小化到托盘**: 关闭窗口不退出,双击托盘恢复
- 🛡️ **渲染崩溃自愈**: 渲染进程崩溃/无响应自动重载(10s 节流)
- ⚙️ **设置面板**: 端口配置 / 开机自启
- 🔄 **自动更新检查**: 启动时检查 GitHub Releases(可跳过版本)
- 🐳 **鲸鱼娘图标**: CC BY-NC-SA 4.0 授权

## 快速开始

### Windows

1. 下载 [最新 Release](https://github.com/1816586742-stack/deepseek-launcher/releases/tag/v0.3.7) 的 `dsh-launcher-v0.3.7_win_x64.zip`
2. 解压到任意目录
3. 双击 `DshLauncher.Windows.exe`

**前置要求**:
- .NET Desktop Runtime 10 (Windows 11 已内置)
- WebView2 Runtime (Windows 10/11 通常已自带)
- Node.js 18+ (dsh 服务需要)

### 从源码构建

```bash
git clone https://github.com/1816586742-stack/deepseek-launcher.git
cd deepseek-launcher
cd DshLauncher.Windows
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## 项目结构

```
DshLauncher.Windows/         # C# + WebView2 (主项目)
  Program.cs                 # 窗口/菜单/托盘/启动动画
  ShellLogic.cs              # 弹窗分类/下载/权限策略(纯逻辑,可测试)
  SessionWatcher.cs          # zstd 会话日志监视
  WatchdogService.cs         # dsh 服务看门狗
  BalanceService.cs          # 余额查询
  ZstdFrames.cs              # zstd 帧结构扫描
  SplashScreen.cs            # 启动动画
  UpdateChecker.cs           # 自动更新检查
  SettingsManager.cs         # 设置持久化
  UpdateDialog.cs            # 更新对话框(Bili23 风格)
  AboutDialog.cs             # 关于对话框
tests/                       # xunit 单元测试 (87 个)
```

## 右键菜单

| 功能 | 快捷键 |
|------|--------|
| Settings | — |
| Balance | — |
| About | — |
| Reload | Ctrl+R |
| DevTools | F12 |
| Fullscreen | F11 |
| Open in Browser | — |
| Open Log Dir | — |
| Exit | — |

## 依赖

| 包 | 版本 | 用途 |
|---|---|---|
| Microsoft.Web.WebView2 | 1.0.4129.50 | WebView2 控件 |
| ZstdSharp.Port | 0.8.8 | zstd 解压(纯托管) |
| xunit | 2.9.3 | 单元测试 |

## 许可

MIT License — Copyright (c) 2026 dsh-launcher contributors

图标: 鲸鱼娘 (CC BY-NC-SA 4.0)
