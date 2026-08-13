#!/bin/bash
# DSH Launcher - Silent dsh starter for macOS/Linux desktop versions
# Called by the native launcher to start dsh in background

LOG="$HOME/.dsh-web.log"
echo "[$(date)] Starting dsh via start-dsh.sh" >> "$LOG"
npx -y @deepseek-ai/dsh web >> "$LOG" 2>&1 &
