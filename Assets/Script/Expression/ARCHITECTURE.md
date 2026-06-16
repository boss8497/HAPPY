# Expression — 수식 연산 라이브러리

게임 데이터(기획 수식)를 런타임에 계산하는 독립 라이브러리.  
`(level + 1) * 10`, `grade * 2`, `Pow(tier, 2)` 같은 수식을 지원한다.

## 중요: Unity 비의존

- Unity 전용 플러그인/라이브러리를 사용하지 않는다 — 순수 C# 구현
- 추후 별도 C# 프로젝트(dll)로 분리 예정
- **이 폴더 하위 파일에 Unity 전용 패키지(VContainer, UniTask 등)를 추가하면 안 됨**
- 예외: `ExpressionPropertyDrawer.cs` 는 Editor 전용이므로 Odin Inspector 사용 허용

---

## 핵심 설계: 사전 컴파일 + 런타임 실행

수식 문자열을 런타임에 직접 파싱하면 느리므로, **두 단계로 분리**한다.

```
[에디터/초기화 시점] 수식 문자열 → Tokenizer → Shunting-yard → RPN 바이트코드
[런타임]            RPN 바이트코드 + Context 변수 주입 → 스택 VM 계산 → double
```

바이트코드(`ExpressionValue[]`)는 MessagePack으로 직렬화하여 저장.  
원본 수식 문자열은 저장하지 않는다.

---

## 처리 흐름

### 1단계 — Tokenization (`Tokenizer.cs`)

문자열을 Token 배열로 분리한다.

| TokenKind | 설명 |
|---|---|
| `Number` | 숫자 리터럴 (double, 과학 기법 `1.5e-2` 지원) |
| `Var` | 변수명 → int key 변환 (`ValueStringKey`로 매핑) |
| `Func` | 함수명 (`pow`, `log`, `min`, `max` 대소문자 무시) |
| `Plus/Minus/Star/Slash` | 이항 연산자 |
| `LParen/RParen` | 괄호 |
| `Comma` | 함수 인자 구분자 |

### 2단계 — Compilation (`Expression.cs` - `CompileInto()`)

**Shunting-yard 알고리즘**으로 토큰을 RPN(역폴란드 표기법) 바이트코드로 변환한다.

연산자 우선순위:

| 연산자 | 우선순위 |
|---|---|
| 단항 `-` (Neg) | 3 (최고) |
| `*`, `/` | 2 |
| `+`, `-` (이항) | 1 |

컴파일 시 최대 스택 깊이(`maxSp`)를 사전 계산해 런타임 stackalloc 크기를 결정한다.

**컴파일 예시:**
```
입력:  (level + 2) * 10
RPN:   PUSH_VAR(level)  PUSH_CONST(2)  ADD  PUSH_CONST(10)  MUL
maxStack: 2
```

### 3단계 — Runtime Execution (`Expression.cs` - `Calc()`)

스택 기반 VM으로 RPN 바이트코드를 순서대로 실행한다.

```
스택: []
→ PUSH_VAR(level)  →  [5]          // ValueContext에서 level=5 주입
→ PUSH_CONST(2)    →  [5, 2]
→ ADD              →  [7]
→ PUSH_CONST(10)   →  [7, 10]
→ MUL              →  [70]
결과: 70.0
```

**스택 메모리 정책:**
- 최대 깊이 ≤ 256: `stackalloc double[n]` (힙 할당 없음)
- 최대 깊이 > 256: `new double[n]` (힙 할당)

---

## Context Pattern (변수 주입)

수식의 변수(level, grade, tier 등)를 런타임에 주입하는 방법.  
`AsyncLocal` 기반 스택 구조로 중첩 가능하고 비동기 안전하다.

### ValueStringKey.cs — 변수명 → int 키 매핑

```csharp
// 사전 등록된 키 (index 고정)
"level" → 0
"grade" → 1
"tier"  → 2
// 런타임에 동적 등록도 지원
```

변수명을 int 키로 변환해 런타임 조회 비용을 최소화한다.

### ValueProvider.cs — 변수 저장

```csharp
var provider = new ValueProvider()
    .Add("level", 10)
    .Add("grade", 5)
    .Add("tier",  2);
```

### ValueContext.cs — 스코프 관리

```csharp
using (new ValueContext(provider)) {
    double result = expression.Calc();  // level=10, grade=5, tier=2
}
// 블록 종료 시 자동 복원
```

- 블록 진입 시 provider를 AsyncLocal 스택에 push, 종료 시 pop
- 미등록 변수는 기본값 0 반환

---

## 지원 연산자 / 함수

**연산자:** `+`, `-` (이항/단항), `*`, `/`

**내장 함수:**

| 함수 | 인자 | 예시 |
|---|---|---|
| `Pow(a, b)` | 2개 | `Pow(tier, 2)` → tier² |
| `Log(x)` / `Log(x, base)` | 1~2개 | `Log(8, 2)` → 3 |
| `Min(a, b, ...)` | 2개 이상 | `Min(level, 10)` |
| `Max(a, b, ...)` | 2개 이상 | `Max(grade, 1)` |

---

## 파일 구조

| 파일 | 역할 |
|---|---|
| `Expression.cs` | 컴파일(`CompileInto`) + 런타임 실행(`Calc`) |
| `Tokenizer.cs` | 문자열 → Token 배열 |
| `ExprFunction.cs` | 함수 enum 정의 및 파싱 |
| `ExpressionValue.cs` | RPN 명령어 구조체 (MessagePack 직렬화) |
| `Context/ValueContext.cs` | AsyncLocal 스코프 관리 |
| `Context/ValueProvider.cs` | 변수명-값 저장소 |
| `Context/ValueStringKey.cs` | 변수명 → int 키 매핑 테이블 |
| `IValue.cs` | 값 인터페이스 |
| `IValueProvider.cs` | 값 제공자 인터페이스 |
| `Editor/ExpressionPropertyDrawer.cs` | Odin Inspector 통합 (역컴파일/편집/재컴파일) |

---

## Editor 지원 (`ExpressionPropertyDrawer.cs`)

Odin Inspector PropertyDrawer로 Inspector에서 수식을 편집할 수 있다.

- **역컴파일**: RPN 바이트코드 → 사람이 읽기 쉬운 수식 문자열 (괄호 자동 삽입)
- **편집**: 텍스트 필드에서 수식 수정
- **재컴파일**: "Compile" 버튼으로 명시적 컴파일
- **에러 표시**: 컴파일 실패 시 에러 메시지 표시 + Revert 버튼

---

## 주요 최적화 포인트

- 수식 문자열을 바이트코드로 사전 컴파일 → 런타임에 파싱 비용 없음
- 변수명을 int 키로 사전 변환 → 딕셔너리 조회 최소화
- `stackalloc` 사용 → 런타임 계산 시 힙 할당 없음
- `AggressiveInlining` 적용 → Calc, EvalFunction 인라인화
- MessagePack으로 바이트코드 직렬화 (원본 문자열 저장 불필요)
