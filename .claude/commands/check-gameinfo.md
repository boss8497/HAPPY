# GameInfo 금지 라이브러리 혼입 점검

`Assets/Script/GameInfo/` 하위 모든 `.cs` 파일을 대상으로 Unity 전용 라이브러리 혼입 여부를 점검한다.

## 점검 규칙

**GameInfo는 Unity와 Server 공용 코드다. 아래 패키지는 절대 사용 불가.**

### 금지 패키지 목록
- `VContainer` — Unity 전용 DI 컨테이너
- `UniTask` — Unity 전용 비동기 라이브러리
- `UniRx` — Unity 전용 Reactive (R3도 GameInfo에서는 금지)
- `Addressables` / `UnityEngine.AddressableAssets` — Unity 에셋 로딩
- `DOTween` / `DG.Tweening` — Unity 트위닝
- `Cinemachine` — Unity 카메라 시스템
- `Unity.Entities` / `Unity.Collections` / `Unity.Mathematics` — Unity ECS/Burst
- `UnityEngine.UI` / `TMPro` — Unity UI
- `Cysharp` (UniTask 포함)

### 허용 패키지
- `UnityEngine` (SerializeField, Serializable 등 순수 직렬화 어트리뷰트 한정)
- `Sirenix.OdinInspector` (Inspector 어트리뷰트, Editor에서만 소비됨)
- `System.*`
- `Newtonsoft.Json`
- `MessagePack`
- `Spine` / `Spine.Unity` — Unity Spine 애니메이션

## 점검 절차

1. Grep으로 `Assets/Script/GameInfo/` 하위 전체 `.cs` 파일에서 금지 패키지 `using` 문 검색
2. 금지 패키지 클래스를 `using` 없이 직접 참조하는 경우도 탐지 (풀네임 사용 패턴)
3. 위반 파일 목록, 위반 라인 번호, 해당 코드를 출력
4. 위반이 없으면 "이상 없음" 으로 결과 출력

## 출력 형식

위반 발견 시:
```
[위반] Assets/Script/GameInfo/Xxx/XxxInfo.cs:12
  using UniTask;  ← 금지: UniTask는 Unity 전용
```

위반 없음 시:
```
GameInfo 금지 라이브러리 혼입 없음. 모든 파일 정상.
```

마지막에 점검한 파일 총 수와 위반 건수를 요약해서 출력한다.
