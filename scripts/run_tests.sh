#!/usr/bin/env bash
# ============================================================================
# 文明演化（Myriad Creation）EditMode 测试入口 —— 本项目 canonical test command
# 用法:
#   bash scripts/run_tests.sh                 # 默认结果输出到 .reverse/canonical_tests_results.xml
#   bash scripts/run_tests.sh <resultsPath>   # 自定义结果路径
# 行为: 跑 Unity Test Framework EditMode 全量测试, 解析结果 XML, 失败时退出码非零
# ============================================================================
set -u

PROJECT="D:/Myriad-Creation-A-Phantasm-of-Passing-Aeons/CivilizationEvolution"
REVERSE="$PROJECT/.reverse"
# 结果路径统一转绝对（Unity 把相对路径解析为项目目录相对，脚本侧按仓库根解析）
RESULTS_ARG="${1:-$REVERSE/canonical_tests_results.xml}"
case "$RESULTS_ARG" in
  /*) RESULTS="$RESULTS_ARG" ;;
  *) RESULTS="$(cd "$(dirname "$RESULTS_ARG")" && pwd)/$(basename "$RESULTS_ARG")" ;;
esac
LOG="$REVERSE/canonical_tests.log"
UNITY="/d/Unity Hub/6000.6.0f1/Editor/Unity.exe"

mkdir -p "$REVERSE"
echo "[run_tests] Unity EditMode 测试启动: $(date '+%H:%M:%S')"
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform EditMode \
    -testResults "$RESULTS" -logFile "$LOG" > /dev/null 2>&1 || true

ERROR_CS=$(grep -cE 'error CS' "$LOG" 2>/dev/null)
ERROR_CS=${ERROR_CS:-0}
echo "[run_tests] ERROR_CS=$ERROR_CS"

python - "$RESULTS" <<'PYEOF'
import sys
import xml.etree.ElementTree as ET
# 安全说明: 仅解析 Unity 本地生成的测试结果 XML（受信输入, 非外部数据）,
# XXE/billion-laughs 不适用; 故使用标准库 ElementTree 而非 defusedxml

path = sys.argv[1]
try:
    root = ET.parse(path).getroot()
except Exception as e:
    print(f"FAIL: 无法解析测试结果 {path}: {e}")
    sys.exit(2)

tests = root.findall('.//test-case')
passed = sum(1 for t in tests if t.get('result') == 'Passed')
failed = [t.get('name') for t in tests if t.get('result') == 'Failed']
print(f"PASSED={passed} FAILED={len(failed)} TOTAL={len(tests)}")
for f in failed:
    print("FAIL:", f)
sys.exit(1 if failed else 0)
PYEOF
TEST_EXIT=$?

if [ "$ERROR_CS" -gt 0 ]; then
    echo "[run_tests] 结果: 存在编译错误（error CS=$ERROR_CS）——失败"
    exit 1
fi
echo "[run_tests] 结果: 测试 $( [ $TEST_EXIT -eq 0 ] && echo 通过 || echo 失败 )（结果 XML: $RESULTS）"
exit $TEST_EXIT
