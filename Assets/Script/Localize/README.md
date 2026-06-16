# Localize — 다국어 텍스트

Unity Localization 패키지를 래핑해 게임 전체에서 다국어 텍스트를 일관되게 조회하는 시스템.  
`AppLifetimeScope`에서 Singleton EntryPoint로 등록, 앱 시작 시 자동 초기화된다.

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Interface/ILocalize.cs` | 인터페이스 |
| `Localize.cs` | 구현체 |
| `Text/LocalizeText.cs` | Inspector 직렬화 가능한 로컬라이즈 텍스트 래퍼 |

## 텍스트 키 형식

```
"TableName/EntryName"
예: "ErrorMessage/NetworkError"
```

## ILocalize 주요 메서드

```csharp
// 키 문자열로 조회
UniTask<string> GetLocalizeText(string term, params object[] arguments)

// 테이블명 + 항목명으로 조회
UniTask<string> GetLocalizeText(string tableName, string entryName, params object[] arguments)

// ErrorMessage 열거형으로 에러 메시지 조회 (타입 안전)
UniTask<string> GetErrorMessage(ErrorMessage errorMessage)
```

## 초기화 흐름

```
AppLifetimeScope 생성 → Initialize() → InitializeAsync() 비동기 실행
    → LocalizationSettings 초기화 완료 대기
    → SelectDefaultLocale("ko")   // 현재 한국어 고정, 향후 시스템 언어 자동 감지 예정
    → ErrorMessage 열거형 → 문자열 딕셔너리 사전 생성 (캐싱)
    → IsInitialized = true
```

## LocalizeText — Inspector 직렬화 래퍼

`LocalizedString`을 Inspector에서 직접 설정할 수 있도록 감싼 클래스.  
UI 컴포넌트의 `[SerializeField]` 필드로 사용한다.

```csharp
[SerializeField] private LocalizeText _label;

// 코드에서 사용
string text = _label.GetText();                    // 동기
string text = await _label.GetTextAsync();         // 비동기
_label.Register(OnTextChanged);                    // 로케일 변경 이벤트 구독
```

| 멤버 | 설명 |
|---|---|
| `IsEmpty` | 테이블/키 미설정 여부 |
| `TableName` | 테이블명 |
| `Key` | 항목 키 |
| `GetText()` | 동기 조회 |
| `GetTextAsync()` | 비동기 조회 |
| `Register(handler)` | 로케일 변경 콜백 등록 |
| `Refresh()` | 문자열 강제 갱신 |

## 에러 메시지 관리

에러 메시지는 `ErrorMessage` 열거형으로 타입 안전하게 참조하고,  
실제 텍스트는 Editor 창(`Tools > ErrorMessage`)에서 로케일별로 관리한다.

- Editor 도구: `Assets/Script/Editor/ErrorMessageEditorWindow.cs`
- 저장 위치: `Assets/Localization/StringTables/`

## 연관 경로

- 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`
- 에러 메시지 Enum: `Assets/Script/GameInfo/Enum/`
- 에디터 관리 도구: `Assets/Script/Editor/ErrorMessageEditorWindow.cs`
