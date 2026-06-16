  # HAPPYproject

  ## 프로젝트 개요
  - Local: C:\Users\Boss315\HAPPYproject
  - GitHub: https://github.com/boss8497/HAPPY
  - Script: HAPPYproject\Assets\Script

  ## 기획 데이터 구조

  ### 기획 데이터 (GameInfo)
  - 위치: `Assets/Script/GameInfo/`
  - `InfoBase`를 상속하고 클래스명 = 파일명, `xxInfo` 접미사 → 기획 데이터 클래스
  - Unity 전용 API 사용 금지 (서버와 공용 가능하도록)
  - 상세 내용: `Assets/Script/GameInfo/ARCHITECTURE.md`

  ### Inspector Attribute
  - 선언: `Assets/Script/GameInfo/Attribute/` — Attribute 클래스만 정의
  - 구현: `Assets/Script/Editor/Attribute/` — PropertyDrawer 구현
  - 상세 내용: `Assets/Script/GameInfo/Attribute/README.md`

  ### 테이블 자동 생성
  - `xxInfo` 클래스에 `[AutoEditorTable(true)]` 추가 후 Unity 메뉴 `Generator > GameInfo > 테이블 자동 생성` 실행
  - 생성 스크립트: `Assets/GAME_INFO_TABLE/Script/Table/Generated/`
  - 에셋 저장 위치: `Assets/GAME_INFO_TABLE/`
  - 상세 내용: `Assets/GAME_INFO_TABLE/README.md`

  ## Rules
  - 커밋 메시지는 한국어로 작성
  - PR 없이 main 브랜치에 직접 push 금지