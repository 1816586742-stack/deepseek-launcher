# DSH Launcher

轻量级 DeepSeek Harness 启动器 — 全平台,零依赖(仅需 Node.js)。

## 版本

| 版本 | 体积 | 适用 |
|---|---|---|
| 脚本版 | 0 KB | 开发者(已有 Node.js) |
| 桌面版 | 672KB~2MB | 普通用户(计划中) |

## 快速开始

### Windows
双击 `scripts/start-dsh.bat`

### macOS / Linux
```bash
chmod +x scripts/start-dsh.sh
./scripts/start-dsh.sh
```

## 工作原理

1. 检查 Node.js 是否安装
2. 通过 `npx -y @deepseek-ai/dsh web` 自动拉起 dsh
3. 等待服务就绪
4. 打开系统浏览器访问 `http://127.0.0.1:3080`

## 前置条件

- [Node.js](https://nodejs.org) 18+
- [DeepSeek API Key](https://platform.deepseek.com)

## 停止服务

- Windows: 关闭命令行窗口,或在任务管理器结束 npx 进程
- macOS/Linux: `Ctrl+C`

## 许可证

MIT
