# GameInfo — 기획 데이터 모델

게임에서 변하지 않는 기획 데이터(설계 데이터)를 정의하는 폴더.  
Unity 전용 코드에 의존하지 않도록 작성하여 서버와 공용으로 사용할 수 있다.

## 네이밍 패턴

- 파일명 = 클래스명 (동일)
- `xxInfo` 접미사 → **기획 데이터 클래스** 라는 의미
  - 예: `CharacterInfo`, `DungeonInfo`, `StatusInfo`

## 기획 데이터 작성 규칙

1. `InfoBase`를 상속한다 (`Assets/Script/GameInfo/Base/InfoBase.cs`)
2. 테이블 자동 생성이 필요하면 클래스에 `[AutoEditorTable(true)]` Attribute를 붙인다
3. **이 폴더의 .cs 파일은 Unity와 Server가 공용으로 사용하는 dll이므로, Unity 전용 패키지를 절대 추가하면 안 된다**
   - 금지: `VContainer`, `UniTask`, `UniRx`, `Addressables`, `DOTween` 등 Unity 전용 패키지
   - 허용: `UnityEngine.SerializeField` 등 기본 UnityEngine, `System.*`, `Newtonsoft.Json`

```csharp
[System.Serializable]
[AutoEditorTable(true)]
public class ExampleInfo : InfoBase {
    // 기획 필드
}
```

## 폴더 구조

| 폴더 | 설명 |
|---|---|
| `Base/` | `InfoBase`, `TableBase`, `IComponent` 등 공통 베이스 |
| `Attribute/` | Unity Inspector 편의용 Attribute 정의 (구현은 `Assets/Script/Editor/Attribute/`) |
| `Character/` | 캐릭터 관련 기획 데이터 |
| `Dungeon/` | 던전/페이즈/액션/트리거 기획 데이터 |
| `Stat/` | 스탯 관련 기획 데이터 |
| `Item/` | 아이템 기획 데이터 |
| `Enum/` | 기획 데이터에서 사용하는 Enum |
| `Component/` | `InfoBase`에 붙이는 컴포넌트 구조 |

## 연관 경로

- 테이블 자동 생성 CodeGenerator: `Assets/GAME_INFO_TABLE/Script/Editor/GameInfoTableCodeGenerator.cs`
- 생성된 테이블 스크립트: `Assets/GAME_INFO_TABLE/Script/Table/Generated/`
- 테이블 에셋(.asset) 저장 위치: `Assets/GAME_INFO_TABLE/`
- Inspector Attribute 구현(Editor): `Assets/Script/Editor/Attribute/`
