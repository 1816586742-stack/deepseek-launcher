#!/bin/bash
# DSH Launcher - macOS/Linux 脚本版
# 双击或终端运行,自动拉起 dsh 并打开浏览器

set -e

echo "正在启动 DeepSeek Harness..."

# 检查 Node.js
if ! command -v node &> /dev/null; then
    echo "[错误] 未找到 Node.js,请先安装: https://nodejs.org"
    exit 1
fi

# 启动 dsh web(后台运行)
npx -y @deepseek-ai/dsh web &
DSH_PID=$!

# 等待端口就绪(最多60秒)
echo "等待 dsh 服务启动..."
for i in $(seq 1 60); do
    if curl -s http://127.0.0.1:3080 > /dev/null 2>&1; then
        break
    fi
    if [ $i -eq 60 ]; then
        echo "[错误] dsh 启动超时"
        kill $DSH_PID 2>/dev/null
        exit 1
    fi
    sleep 1
done

echo "dsh 已启动,正在打开浏览器..."

# 跨平台打开浏览器
if command -v open &> /dev/null; then
    open http://127.0.0.1:3080      # macOS
elif command -v xdg-open &> /dev/null; then
    xdg-open http://127.0.0.1:3080  # Linux
else
    echo "请手动打开: http://127.0.0.1:3080"
fi

echo ""
echo "按 Ctrl+C 停止 dsh 服务"
wait $DSH_PID
