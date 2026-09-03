#!/usr/bin/env bash
# PlayMode 测试（第二关：运行时无异常）——真实生命周期
set -u
PROJECT="D:/Myriad-Creation-A-Phantasm-of-Passing-Aeons/CivilizationEvolution"
REVERSE="$PROJECT/.reverse"
RESULTS="$REVERSE/playmode_tests_results.xml"
LOG="$REVERSE/playmode_tests.log"
UNITY="/d/Unity Hub/6000.6.0f1/Editor/Unity.exe"
mkdir -p "$REVERSE"
echo "[run_playmode] PlayMode 测试启动: $(date '+%H:%M:%S')"
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode \
    -testResults "$RESULTS" -logFile "$LOG" > /dev/null 2>&1 || true
echo "[run_playmode] ERROR_CS=$(grep -cE 'error CS' "$LOG" 2>/dev/null || echo 0)"
python - "$RESULTS" <<'PYEOF'
import sys, xml.etree.ElementTree as ET
# 安全说明: 仅解析 Unity 本地生成的测试结果 XML（受信输入, 非外部数据）,
# XXE/billion-laughs 不适用; 与 run_tests.sh 同模式
try:
    root = ET.parse(sys.argv[1]).getroot()
    passed = int(root.get('passed', 0)); failed = int(root.get('failed', 0))
    print(f"[run_playmode] PASSED={passed} FAILED={failed} TOTAL={passed+failed}")
    for tc in root.iter('test-case'):
        if tc.get('result') == 'Failed':
            msg = tc.find('failure/message')
            print(f"FAIL: {tc.get('name')}: {(msg.text or '').strip()[:200]}")
    sys.exit(1 if failed > 0 else 0)
except Exception as e:
    print(f"[run_playmode] 结果解析失败: {e}")
    sys.exit(1)
PYEOF
echo "[run_playmode] RUN_PLAY_EXIT=$?"
