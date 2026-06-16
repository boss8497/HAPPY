# Editor — 에디터 전용 도구

Unity Editor에서만 동작하는 커스텀 도구 모음. 런타임에는 포함되지 않는다.

## 폴더 구조

| 폴더/파일 | 역할 |
|---|---|
| `Attribute/` | GameInfo Attribute의 PropertyDrawer 구현 (각 `XxxAttribute` → `XxxDrawer`) |
| `ValueDrawer/SerializeGuidDrawer.cs` | `SerializeGuid` Inspector 커스텀 드로어 |
| `NavigationMenu.cs` | 에디터 상단 메뉴 도구 |
| `ErrorMessageEditorWindow.cs` | 에러 메시지 로컬라이즈 관리 창 |

## Attribute/ — PropertyDrawer 구현

`Assets/Script/GameInfo/Attribute/`에 선언된 Attribute의 Inspector 렌더링 구현.  
대응 관계: `XxxAttribute.cs` → `Editor/Attribute/XxxDrawer.cs`

각 Drawer는 Odin Inspector `OdinAttributeDrawer` 또는 `OdinValueDrawer` 기반.  
Inspector에서 기획 데이터를 편집할 때 드롭다운, 유효성 검사, 경로 선택 등 편의 기능을 제공한다.

## NavigationMenu.cs

Unity 메뉴 `Tools > 데이터 리로드` — 에디터에서 기획 데이터 변경 후 즉시 재로드.

```
GameInfoManager.Instance.Release() → Load()
```

## ErrorMessageEditorWindow.cs

메뉴 `Tools > ErrorMessage` — 에러 메시지 로컬라이즈 테이블 관리 창 (Odin Inspector 기반).

- `ErrorMessage` 열거형 값 기반으로 테이블 항목 자동 생성
- 로케일별 StringTable 자동 생성/관리 (`Assets/Localization/StringTables/`)
- 변경사항 저장 버튼 제공

## SerializeGuidDrawer.cs

`SerializeGuid` 필드를 가진 모든 Inspector에 자동 적용.

```
[Label]  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  [Reset] [New]
```

- 텍스트 필드: 읽기 전용 (오타 방지)
- Reset: `Guid.Empty`로 초기화
- New: 새 Guid 발급

## 연관 경로

- Attribute 선언: `Assets/Script/GameInfo/Attribute/`
- SerializeGuid: `Assets/Script/Guid/SerializeGuid.cs`
- 에러 메시지 Enum: `Assets/Script/GameInfo/Enum/`
