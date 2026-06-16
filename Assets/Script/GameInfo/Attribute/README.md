# GameInfo/Attribute — Inspector 편의 Attribute 정의

Unity Inspector에서 기획 데이터를 편하게 편집하기 위한 Attribute를 정의하는 폴더.

## 주의

- 이 폴더는 **Attribute 클래스 선언만** 포함한다
- 실제 Inspector 렌더링 구현(PropertyDrawer)은 `Assets/Script/Editor/Attribute/` 에 있다
- 대응 관계: `XxxAttribute.cs` → `Assets/Script/Editor/Attribute/XxxDrawer.cs`

## Attribute 목록

| Attribute | 용도 |
|---|---|
| `AutoEditorTableAttribute` | 붙인 InfoBase 서브클래스를 대상으로 테이블을 자동 생성 |
| `LocalizePathAttribute` | 로컬라이즈 키 경로를 인스펙터에서 선택 |
| `AssetPathAttribute` | 에셋 경로를 인스펙터에서 선택 |
| `BehaviourAttribute` | 비헤이비어 ID를 인스펙터에서 선택 |
| `CharacterAttribute` | 캐릭터 UID를 인스펙터에서 선택 |
| `StatusAttribute` | 스탯 UID를 인스펙터에서 선택 |
| `NextNodeAttribute` | 다음 노드를 인스펙터에서 선택 |
| `DungeonAttribute` | 던전 UID를 인스펙터에서 선택 |
| `PhaseAttribute` | 페이즈 UID를 인스펙터에서 선택 |
| `ItemAttribute` | 아이템 UID를 인스펙터에서 선택 |
