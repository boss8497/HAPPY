# GAME_INFO_TABLE — 기획 데이터 테이블

`InfoBase`를 상속한 `xxInfo` 클래스에 `[AutoEditorTable]`을 붙이면  
CodeGenerator가 자동으로 테이블 스크립트를 생성하고, 에셋은 이 폴더에 저장된다.

## 테이블 자동 생성 흐름

1. `xxInfo` 클래스에 `[AutoEditorTable(true)]` 추가
2. Unity 메뉴 → `Generator > GameInfo > 테이블 자동 생성` 실행
3. `Script/Table/Generated/xxTable.generated.cs` 생성됨
4. `xxTable.asset` 에셋을 이 폴더에 생성하여 데이터 입력

CodeGenerator 위치: `Script/Editor/GameInfoTableCodeGenerator.cs`

## 폴더 구조

| 경로 | 설명 |
|---|---|
| `Script/Table/` | 수동으로 작성된 테이블 클래스 |
| `Script/Table/Generated/` | CodeGenerator가 자동 생성한 테이블 클래스 (수정 금지) |
| `Script/Single/` | 테이블이 아닌 단일 설정 데이터 (예: `GameConfiguration`) |
| `Script/Editor/` | 테이블 에디터 창 및 CodeGenerator |
| `Script/Interface/` | `IGameInfoManager` 등 인터페이스 |
| `*.asset` | 실제 기획 데이터가 저장되는 ScriptableObject 에셋 |

## 네이밍 규칙

- 기획 데이터 클래스: `xxInfo` → 테이블 클래스: `xxTable` → 에셋: `xxTable.asset`
- 자동 생성 파일은 `.generated.cs` 접미사를 가진다 (직접 수정 금지)
