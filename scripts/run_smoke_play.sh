#!/usr/bin/env bash
# Play 模式冒烟（第二关：运行时无异常）——SmokeTest.RunPlay
set -u
PROJECT="D:/Myriad-Creation-A-Phantasm-of-Passing-Aeons/CivilizationEvolution"
REVERSE="$PROJECT/.reverse"
LOG="$REVERSE/smoke_play.log"
UNITY="/d/Unity Hub/6000.5.8f1/Editor/Unity.exe"
mkdir -p "$REVERSE"
echo "[smoke_play] Play 冒烟启动: $(date '+%H:%M:%S')"
"$UNITY" -batchmode -projectPath "$PROJECT" \
    -executeMethod CivilizationEvolution.EditorTools.SmokeTest.RunPlay \
    -logFile "$LOG" > /dev/null 2>&1 || true
RESULT="$PROJECT/Temp/smoke_result.txt"
if [ -f "$RESULT" ]; then
    echo "===== 冒烟结果 ====="
    cat "$RESULT"
    grep -q "^PLAY-OK" "$RESULT" && grep -q "runtimeErrorCount=0" "$RESULT" \
        && echo "[smoke_play] 第二关通过（Play 运行无异常）" \
        || echo "[smoke_play] 第二关失败（详见结果）"
else
    echo "[smoke_play] 无结果文件——RunPlay 未完成"
    grep -E "error CS|Exception" "$LOG" | head -5
fi
