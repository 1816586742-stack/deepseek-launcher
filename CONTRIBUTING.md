# Contributing

欢迎贡献!以下是参与方式:

## 报告问题

在 [GitHub Issues](https://github.com/1816586742-stack/dsh-launcher-cross/issues) 提交 bug 报告或功能建议。

## 提交代码

1. Fork 仓库
2. 创建功能分支: `git checkout -b feature/my-feature`
3. 提交更改: `git commit -m 'Add my feature'`
4. 推送分支: `git push origin feature/my-feature`
5. 创建 Pull Request

## 开发环境

### Windows
```bash
cd DshLauncher.Windows
dotnet run
```

### macOS
```bash
cd DshLauncher.MacOS
swiftc main.swift -o dsh-launcher -framework Cocoa -framework WebKit
./dsh-launcher
```

### Linux
```bash
cd DshLauncher.Linux
pip install pygobject
python3 launcher.py
```

## 代码规范

- Windows: C# 遵循 .NET 风格
- macOS: Swift 遵循 Apple 风格
- Linux: Python 遵循 PEP 8
- 提交信息使用英文,简洁明了

## 许可证

贡献即表示您同意您的代码在 MIT 许可证下发布。
