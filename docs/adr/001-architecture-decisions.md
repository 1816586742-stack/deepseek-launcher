# ADR-001: DSH Launcher 架构决策

**日期**: 2026-08-14
**状态**: 已批准
**决策者**: 项目维护者

## 背景

DeepSeek Harness (dsh) 缺少官方的桌面启动器。社区已有 dsh-launcher (Ruler4396) 仅支持 Windows,需要一个全平台方案。

## 决策

### 1. 项目定位
跨平台(Windows/macOS/Linux)轻量级启动器,作为 dsh-launcher 的补充而非替代。

### 2. 技术架构
每个平台使用原生最轻量方案:
- Windows: C# + WebView2
- macOS: Swift + WKWebView
- Linux: Python + GTK4 + WebKitGTK
- 脚本版: batch/bash(0 依赖)

### 3. dsh 集成方式
不打包 dsh,通过 `npx -y @deepseek-ai/dsh web` 按需拉取(永远最新,插件兼容)。

### 4. 用户分层
- 脚本版:开发者(已有 Node.js)
- 桌面版:普通用户(需要编译版)

### 5. 发布策略
- GitHub Releases 唯一发布渠道
- GitHub Actions CI 自动编译(tag 触发)
- 语义化版本(v0.x.x)

### 6. 商标处理
使用 DeepSeek 官方 logo + 免责声明,暂不主动联系官方。

### 7. 维护模式
社区驱动,欢迎 PR,明确维护承诺。

### 8. 质量策略
快速迭代优先,测试后续补充。

## 后果

### 正面
- 体积最小(642 KB vs 135 MB Electron)
- 插件完全兼容
- 三平台统一架构

### 负面
- 三个平台三种语言,维护成本高
- 没有测试,质量风险
- 商标风险(免责声明不能完全规避)

## 相关决策
- DSH Shell (Electron 版)作为学习成果保留
- dsh-launcher (Ruler4396) 作为 Windows 参考实现
