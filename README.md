# HAPPY Project

Unity 6 기반 개인 포트폴리오 프로젝트. 장르는 러닝 게임이지만, 목표는 게임 자체보다 **여러 장르에 재사용 가능한 클라이언트 아키텍처**를 처음부터 끝까지 혼자 설계하는 것입니다.

- GitHub 저장소: [boss8497/HAPPY](https://github.com/boss8497/HAPPY)
- 데모 영상: [플레이 영상](https://youtu.be/cPMAi_6IVBo) / [최적화 작업 영상](https://youtu.be/yu9KNhfChl4)
- 빌드: [Windows 빌드 다운로드](https://drive.google.com/file/d/1Hp_Nag2OQNoWnqUTHR-A6GmTdxsX61g7/view?usp=sharing) [APK 다운로드](https://drive.google.com/file/d/1SDlyjQkX_kLM3wCini6YGiWT7o8IOCrq/view?usp=sharing)
- Notion: [이력서](https://sprinkle-quesadilla-28e.notion.site/3c57f18c7c7080f49daac2604fcdc5fa) / [경력기술서](https://sprinkle-quesadilla-28e.notion.site/3c57f18c7c7080f49f2ac41962de395a) / [포트폴리오](https://sprinkle-quesadilla-28e.notion.site/3c57f18c7c7080448b02e26b6f3b0687)

## 프로젝트 목표

- 서버 합류를 전제로, 클라이언트-서버 경계를 처음부터 분리한 구조 설계
- Unity ECS/Burst를 실제 게임플레이(이동/점프/충돌)에 적용
- 기획 데이터 → 반응형 데이터 레이어 → 게임플레이 로직으로 이어지는 4계층 아키텍처 구현
- Unity 기본 컴포넌트(Button, Screen 전환 등)를 프로젝트 요구에 맞게 직접 재구현

## 기술 스택

Unity6, C#, ECS/Burst, VContainer(DI), R3(Reactive), MessagePack, UniTask, Addressable, DOTween, Spine, Odin Inspector

## 아키텍처 개요

```
FileStorage(JSON/MessagePack)
    → GameDataBase (CRUD)
    → GameData/Model (불변 구조체, MessagePack)
    → GameData/Data  (R3 반응형 래퍼)
    → GamePlay/Service (비즈니스 로직)
    → GamePlay/UI, Logic
```

Unity와 서버가 공유할 수 있는 코드(`GameInfo`)와 Unity 클라이언트 전용 코드(`GamePlay`)를 폴더 단위로 엄격히 분리했습니다. `GameInfo` 하위에는 VContainer, UniTask, Addressables 등 Unity 전용 라이브러리를 쓸 수 없고, 이 규칙은 [GitHub Actions CI](.github/workflows/check-gameinfo.yml)로 커밋마다 자동 검사합니다.

| 영역 | 경로 | 설명 |
|---|---|---|
| 기획 데이터 (서버 공용) | [`Assets/Script/GameInfo`](Assets/Script/GameInfo/ARCHITECTURE.md) | `InfoBase` 상속 기획 데이터. Unity 비의존 |
| 게임플레이 (Client 전용) | [`Assets/Script/GamePlay`](Assets/Script/GamePlay/ARCHITECTURE.md) | FSM, ECS, Factory 패턴으로 기획 데이터를 실제 오브젝트로 변환 |
| 런타임 데이터 레이어 | [`Assets/Script/GameData`](Assets/Script/GameData/ARCHITECTURE.md) | Model(불변 구조체) → Data(R3 ReactiveProperty 래퍼) |
| 로컬 저장소 | [`Assets/Script/DataBase`](Assets/Script/DataBase/ARCHITECTURE.md) | 서버 부재 기간 동안 Client가 직접 담당하는 저장/로드 |
| UI/Screen 관리 | [`Assets/Script/GUI`](Assets/Script/GUI/ARCHITECTURE.md) | LinkedList 기반 Screen 스택, Layer 시스템 |
| DI 계층 구조 | [`Assets/Script/LifetimeScope`](Assets/Script/LifetimeScope/ARCHITECTURE.md) | VContainer 기반 App → Client → Group → Stage 4단계 Scope |
| 수식 계산 엔진 | [`Assets/Script/Expression`](Assets/Script/Expression/ARCHITECTURE.md) | 수식 문자열을 RPN 바이트코드로 컴파일해 스택 VM으로 실행 (Unity 비의존) |
| 공통 유틸리티 | [`Assets/Script/Utility`](Assets/Script/Utility/ARCHITECTURE.md) | 오브젝트 풀링 등 |

각 폴더의 `ARCHITECTURE.md`에 설계 배경과 세부 구현이 정리되어 있습니다.

## 눈여겨볼 만한 구현

- **ECS + Burst 이동/충돌**: Default World 대신 별도 World를 만들어 Managed 코드와 계산 영역을 분리, Unity 기본 충돌 대신 진행 방향 기준으로 직접 판정 ([`Assets/Script/GamePlay/ECS`](Assets/Script/GamePlay/ECS))
- **UI 시스템 직접 구축**: "특정 화면 뒤에 열기", "특정 화면만 닫기"에 대응하려 Stack 대신 LinkedList 기반 Screen 스택으로 구현하고, Unity 기본 Button/Toggle 대신 상호작용만 남긴 경량 컴포넌트로 prefab 용량을 줄임 ([`Assets/Script/GUI`](Assets/Script/GUI/ARCHITECTURE.md))
- **R3 반응형 데이터 계층**: Model(불변 구조체) → Data(R3 래퍼) → Service → View로 계층을 나누고, 구독을 `DisposableBag`으로 화면 수명에 묶어 `event +=` 방식에서 반복되던 구독 해제 누락을 없앰. 서버가 붙으면 Model을 채우는 경로만 교체되고 UI 바인딩은 유지 ([`Assets/Script/GameData`](Assets/Script/GameData/ARCHITECTURE.md))
- **비동기 FSM 캐릭터 런타임**: `GameInfo`의 노드/전환 기획 데이터를 읽어 UniTask 기반 비동기 FSM으로 실행, Node/Transition은 `ClassPool`로 재사용해 GC 압박 최소화 ([`Assets/Script/GamePlay/Character`](Assets/Script/GamePlay/Character))
- **MonoBehaviour 없는 카메라 제어**: 경계 계산, Shake, 스피드 버프 줌을 순수 C# 클래스로 구현해 `StageLifetimeScope`가 VContainer로 주입 — 씬 GameObject 없이 테스트/교체가 쉬운 구조 ([`Assets/Script/GamePlay/Camera`](Assets/Script/GamePlay/Camera))
- **튜토리얼 스포트라이트 시스템**: 대상 UI 위에 겹치는 투명 대리 버튼으로 클릭을 가로챈 뒤 실제 버튼에 전달, 4방향 마스크로 별도 shader 없이 스포트라이트 연출, 엑셀 데이터로 기획자가 순서를 직접 구성 ([`Assets/Script/GamePlay/Tutorial`](Assets/Script/GamePlay/Tutorial))
- **Addressable 중앙 캐시**: RefCount + 유예시간(grace period) 기반 캐시로 여러 시스템(Audio/Spine/GameSetting)이 같은 에셋을 로드해도 실제 로드는 한 번만, 사용이 끝나도 일정 시간 재사용 대기 후 해제 ([`Assets/Script/Addressable`](Assets/Script/Addressable/README.md))
- **런타임 디버그 에디터 창 4종**: Screen 스택 / Addressable 캐시 / 오브젝트 풀 / 클래스 풀의 내부 상태를 리플렉션으로 들여다보는 Editor 창. 필드 리네임에 조용히 깨지는 문제를 `nameof` 기반 상수 참조로 방어 ([`Assets/Script/Editor`](Assets/Script/Editor/README.md))
- **수식 연산 엔진**: 수식 문자열을 Shunting-yard로 RPN 바이트코드 컴파일 후 `stackalloc` 기반 스택 VM으로 실행(런타임 힙 할당 없음), Unity 비의존 순수 C# ([`Assets/Script/Expression`](Assets/Script/Expression))

## 더 살펴보기

핵심 아키텍처 외에, 각자 하나의 역할만 담당하는 작은 지원 모듈들입니다.

| 모듈 | 역할 |
|---|---|
| [`Assets/Script/Client`](Assets/Script/Client) | 서버 통신 인터페이스 (`IClient`) — 현재는 로컬 DB로 동작, 서버 완성 시 구현체만 교체 |
| [`Assets/Script/GameSetting`](Assets/Script/GameSetting) | 프레임레이트/V-Sync 등 실행 설정을 Addressable 에셋에서 로드 |
| [`Assets/Script/GameTimer`](Assets/Script/GameTimer) | 일시정지 상태를 반영하는 앱 전역 타이머 |
| [`Assets/Script/Localize`](Assets/Script/Localize) | Unity Localization 패키지 래퍼 |
| [`Assets/Script/SceneLoader`](Assets/Script/SceneLoader) | Addressable Additive 로드 기반 씬 전환 |
| [`Assets/Script/Guid`](Assets/Script/Guid) | Unity가 직렬화 못 하는 `System.Guid`를 `uint` 4개로 분할해 래핑 |
