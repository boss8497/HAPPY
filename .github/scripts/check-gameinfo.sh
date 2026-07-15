#!/usr/bin/env bash
# Assets/Script/GameInfo/ 는 Unity/Server 공용 코드이므로
# Unity 전용 라이브러리 사용을 금지한다. (.claude/commands/check-gameinfo.md 규칙 기반)
set -euo pipefail

TARGET_DIR="Assets/Script/GameInfo"

if [ ! -d "$TARGET_DIR" ]; then
  echo "대상 디렉토리를 찾을 수 없음: $TARGET_DIR"
  exit 1
fi

FORBIDDEN_PATTERN='\bVContainer\b|\bUniTask\b|\bUniRx\b|\bAddressables\b|UnityEngine\.AddressableAssets|\bDOTween\b|DG\.Tweening|\bCinemachine\b|Unity\.Entities|Unity\.Collections|Unity\.Mathematics|UnityEngine\.UI|\bTMPro\b|\bCysharp\b'

TOTAL_FILES=$(find "$TARGET_DIR" -name '*.cs' | wc -l)

echo "GameInfo 금지 라이브러리 혼입 점검 시작 (대상 파일 ${TOTAL_FILES}개)"
echo ""

MATCHES=$(grep -rnE --include='*.cs' "$FORBIDDEN_PATTERN" "$TARGET_DIR" || true)

if [ -n "$MATCHES" ]; then
  echo "[위반 발견]"
  echo "$MATCHES"
  echo ""
  echo "점검 파일 수: ${TOTAL_FILES} / 위반 건수: $(echo "$MATCHES" | wc -l)"
  exit 1
fi

echo "GameInfo 금지 라이브러리 혼입 없음. 모든 파일 정상."
echo "점검 파일 수: ${TOTAL_FILES} / 위반 건수: 0"
