  # HAPPYproject

  ## 프로젝트 개요
  - Local: C:\Users\Boss315\HAPPYproject
  - GitHub: https://github.com/boss8497/HAPPY
  - Script: HAPPYproject\Assets\Script

  ## 기획 데이터 구조

  ### 기획 데이터 (GameInfo)
  - 위치: `Assets/Script/GameInfo/`
  - `InfoBase`를 상속하고 클래스명 = 파일명, `xxInfo` 접미사 → 기획 데이터 클래스
  - **Unity와 Server 공용 .cs / dll — `Assets/Script/GameInfo/` 하위 파일에는 Unity 전용 라이브러리를 절대 추가하면 안 됨**
  - 금지 대상 예시: VContainer, UniTask, UniRx, Addressables, DOTween 등 Unity 전용 패키지
  - 허용: `UnityEngine.SerializeField`, `System.*`, `Newtonsoft.Json` 등 서버에서도 사용 가능한 것
  - 상세 내용: `Assets/Script/GameInfo/ARCHITECTURE.md`

  ### GamePlay (Client 전용 구현)
  - 위치: `Assets/Script/GamePlay/`
  - Unity Client에서만 사용. VContainer, UniTask, ECS 등 Unity 전용 라이브러리 자유롭게 사용 가능
  - `GameInfo` 기획 데이터를 읽어 Factory 패턴으로 Client 객체를 생성해 사용
  - 상세 내용: `Assets/Script/GamePlay/ARCHITECTURE.md`

  **주요 패턴**
  - Factory: `ActionFactory`, `TriggerFactory` — CodeGenerator가 switch 분기 자동 생성 (`*.CodeGen.cs` 직접 수정 금지)
  - FSM: `ClientXxxNode` (상태) + `ClientXxxTransition` (전환 조건) — Begin/Update/End 시점 전환, Priority 우선순위
  - ECS: 이동/점프/충돌을 Burst 컴파일 시스템으로 처리 (`ECS/` 폴더)
  - Partial Class: `Character`, `StageManager` 등 관심사별 파일 분리

  **네이밍**
  - FSM Node: `ClientXxxNode`, FSM Transition: `ClientXxxTransition`
  - Action/Trigger: `ClientXxxAction`, `ClientXxxTrigger`

  ### GameData — 런타임 데이터 레이어
  - 위치: `Assets/Script/GameData/`
  - `Model/`: DB 저장 및 서버 전송용 불변 구조체 — MessagePack `[Key(n)]` 어트리뷰트 필수
  - `Data/`: Model을 R3 `ReactiveProperty`로 감싼 반응형 래퍼 — `IData<T>` 구현
  - Model을 직접 사용하지 않고 Data를 통해 접근한다 (Service 계층에서 소비)
  - 상세 내용: `Assets/Script/GameData/ARCHITECTURE.md`

  ### DataBase — Client 측 저장소
  - 위치: `Assets/Script/DataBase/`
  - Server가 없는 동안 Client에서 직접 저장/로드 담당 (Server 완성 시 교체 전제)
  - 현재 **JSON으로 저장** (디버깅 편의), 서버 패킷 전송 시 **MessagePack** 사용 예정
  - `IDataBase` 인터페이스 — Load/Save + 아이템 CRUD (uid 자동 발급, 스택 합산, 레벨업)
  - `FileStorage`: `Application.persistentDataPath` 기준 비동기 파일 I/O
  - 상세 내용: `Assets/Script/DataBase/ARCHITECTURE.md`

  **전체 데이터 흐름**
  ```
  FileStorage (JSON/MessagePack)
      → GameDataBase (CRUD)
      → GameData/Model (불변 구조체)
      → GameData/Data  (R3 반응형 래퍼)
      → GamePlay/Service (비즈니스 로직)
      → GamePlay UI / Logic
  ```

  ### Inspector Attribute
  - 선언: `Assets/Script/GameInfo/Attribute/` — Attribute 클래스만 정의
  - 구현: `Assets/Script/Editor/Attribute/` — PropertyDrawer 구현
  - 상세 내용: `Assets/Script/GameInfo/Attribute/README.md`

  ### 테이블 자동 생성
  - `xxInfo` 클래스에 `[AutoEditorTable(true)]` 추가 후 Unity 메뉴 `Generator > GameInfo > 테이블 자동 생성` 실행
  - 생성 스크립트: `Assets/GAME_INFO_TABLE/Script/Table/Generated/`
  - 에셋 저장 위치: `Assets/GAME_INFO_TABLE/`
  - 상세 내용: `Assets/GAME_INFO_TABLE/README.md`

  ### GUI — Screen 관리 시스템
  - 위치: `Assets/Script/GUI/`
  - `AppLifetimeScope`에서 Addressable로 프리팹 Instantiate → 게임 실행 직후부터 앱 전체 생존
  - 상세 내용: `Assets/Script/GUI/ARCHITECTURE.md`

  **핵심 설계**
  - **LinkedList 기반 Stack**: "특정 UI 뒤에 열기", "특정 UI만 닫기" 요청에 유연하게 대응하기 위해 Stack 대신 LinkedList 채택 (Stack 동작 지향)
  - **DontClose Screen**: 리스트 앞쪽에 고정 배치. `force: true`가 아니면 닫히지 않음 (HUD, Navigation 등)
  - **Layer 시스템**: `HUD(0) → None(1) → Popup(2) → Overlay(3) → StageTransition(4) → Loading(5) → SafeArea(6)` 순서로 렌더링
  - **SafeArea**: Screen 열리는 동안 최상위 레이어로 입력 차단
  - **Queue 처리**: 다중 Open/Close 요청을 순서대로 처리. `OpenAsync()` await 시 화면이 완전히 열릴 때까지 대기 보장
  - **캐싱**: 한 번 열린 Screen은 Close 후에도 `_loadedScreens`에 유지 → 재오픈 시 로딩 없음
  - **씬 이동 시**: `CloseAllAsync(force:true)` → `ResourceClear()` 로 전체 Destroy

  **ViewModel**: ListView Element 데이터 바인딩용. `SelectElement` + `Selector` 패턴으로 선택 상태 중앙 관리. R3 ReactiveProperty로 데이터 → UI 자동 갱신

  **ScreenKey**: `[ScreenKey]` 어트리뷰트 — Inspector 드롭다운으로 Screen 선택 (문자열 오타 방지)

  ### LifetimeScope — VContainer DI 계층 구조
  - 위치: `Assets/Script/LifetimeScope/`
  - VContainer 기반 DI. Scope를 4단계 계층으로 나눠 필요한 시점에 생성/파괴
  - 상세 내용: `Assets/Script/LifetimeScope/ARCHITECTURE.md`

  **Scope 계층 (`ScopeType` enum 순서 = 계층 순서)**
  ```
  App (StartUp 씬 GameObject) → Client (동적 생성) → Group (동적 생성) → Stage (GameScene GameObject)
  ```
  - `App`, `Client`, `Group`: ScopeFactory.CreateScope()로 동적 생성
  - `Stage`만 예외 — GameScene 씬 안에 GameObject 컴포넌트로 직접 배치
    - 이유: `mainCamera`, `CinemachineCamera` 등 씬 GameObject를 Inspector에서 직접 참조해야 함
  - `ScopeLocator`: Dictionary 기반 Scope 중앙 관리. SetScope() 호출 시 하위 Scope 전체 자동 Dispose

  **씬 실행 순서 및 Scope 생성 주체**
  ```
  StartUp (AppScope) → Title (ClientScope 생성) → Lobby (GroupScope 생성) → GameScene (StageScope 자동)
  ```
  - StartUpLogic: Addressable → GameSetting 초기화 후 ClientScope 생성 → Title 씬 전환
  - TitleHUD: "Start" 클릭 → GroupScope 생성 → Lobby 씬 전환
  - GameScene: 씬 로드 시 StageLifetimeScope 자동 초기화, 씬 언로드 시 OnDestroy()로 해제
  - 씬별 로직: `Assets/Script/Scene/` (상세: `README.md`)

  **IClient**: 서버 통신 인터페이스. 현재 `GameClient`가 로컬 DB로 동작 (Server 구현 시 교체 예정)
  - `Assets/Script/Client/`

  **SceneLoader**: Addressable 기반 Additive 로드 → Active 씬 전환 → 이전 씬 UnloadAsync
  - `Assets/Script/SceneLoader/`

  ### Guid — SerializeGuid
  - 위치: `Assets/Script/Guid/`
  - Unity는 `System.Guid` 직렬화를 미지원 → Guid 16바이트를 `uint` 4개로 분할해 래핑한 구조체
  - `ISerializationCallbackReceiver`로 직렬화 전/후 자동 변환, `_cacheValid`로 Guid 객체 캐시
  - `Guid`와 암묵적 변환(implicit) 가능 — 기존 `Guid` 타입과 혼용 가능
  - 주요 사용처: FSM `NodeBase.guid`, `TransitionBase.nextNodeGuid` (노드 간 GUID 참조)
  - Editor Drawer: Inspector에서 읽기 전용 표시 + Reset/New 버튼 (`SerializeGuidDrawer.cs`)
  - 상세 내용: `Assets/Script/Guid/README.md`

  ### Expression — 수식 연산 라이브러리
  - 위치: `Assets/Script/Expression/`
  - `(level + 1) * 10`, `Pow(tier, 2)` 등의 수식을 계산하는 독립 라이브러리
  - **Unity 비의존** — 순수 C# 구현, 추후 별도 dll 프로젝트로 분리 예정
  - **이 폴더 하위에 Unity 전용 패키지 추가 금지** (예외: `Editor/` 폴더 내 파일)
  - 상세 내용: `Assets/Script/Expression/ARCHITECTURE.md`

  **핵심 설계: 사전 컴파일 + 런타임 실행**
  - 컴파일: 수식 문자열 → Shunting-yard 알고리즘 → RPN 바이트코드(`ExpressionValue[]`)
  - 런타임: RPN 바이트코드를 스택 VM으로 실행 (`stackalloc` 사용, 힙 할당 없음)
  - 바이트코드는 MessagePack으로 직렬화 저장 (원본 문자열 저장 안 함)

  **Context Pattern (변수 주입)**
  - `ValueStringKey`: 변수명(`level`, `grade`, `tier`) → int 키 사전 매핑
  - `ValueProvider`: 변수명-값 저장
  - `ValueContext`: `using` 블록으로 스코프 관리 (AsyncLocal 기반, 중첩 가능)

  ```csharp
  using (new ValueContext(new ValueProvider().Add("level", 10).Add("grade", 5))) {
      double result = expression.Calc();
  }
  ```

  **지원:** 연산자 `+`, `-`, `*`, `/`, 단항 `-` / 함수 `Pow`, `Log`, `Min`, `Max`

  ### Addressable — Addressable Assets 관리
  - 위치: `Assets/Script/Addressable/`
  - Unity Addressable 시스템 래퍼. 초기화, 카탈로그 업데이트, 다운로드, 인터넷 연결 확인 담당
  - 앱 시작 시 가장 먼저 초기화 (`StartUpLogic`에서 호출)
  - 인터넷 확인: `Application.internetReachability` → `connectivitycheck.gstatic.com/generate_204` 실제 요청
  - 상세 내용: `Assets/Script/Addressable/README.md`

  ### Client — 서버 통신
  - 위치: `Assets/Script/Client/`
  - `IClient` 인터페이스 + `GameClient` 구현체 (Partial Class: 초기화 / 통신 로직 분리)
  - 현재 Server 없음 → `GameClient`가 로컬 DB로 동작. Server 완성 시 구현체만 교체
  - `Req_ClearStage`: 검증 → 보상 계산 → 던전 진행도 업데이트 → DB 저장 → 보상 반환
  - 상세 내용: `Assets/Script/Client/README.md`

  ### Editor — 에디터 전용 도구
  - 위치: `Assets/Script/Editor/`
  - 런타임 미포함. Attribute PropertyDrawer, 커스텀 에디터 창, 메뉴 도구 모음
  - `Attribute/`: GameInfo Attribute의 Odin PropertyDrawer 구현 (`XxxAttribute` → `XxxDrawer`)
  - `NavigationMenu`: `Tools > 데이터 리로드` — GameInfoManager 재로드
  - `ErrorMessageEditorWindow`: `Tools > ErrorMessage` — 에러 메시지 로케일별 텍스트 관리
  - 상세 내용: `Assets/Script/Editor/README.md`

  ### GameSetting — 게임 설정
  - 위치: `Assets/Script/GameSetting/`
  - 프레임레이트(`targetFrameRate`), V-Sync(`vSyncCount`) 설정을 Addressable 에셋에서 로드해 적용
  - 로드 실패 시 Exception — 설정 없이 게임이 진행되지 않도록 의도된 설계
  - 상세 내용: `Assets/Script/GameSetting/README.md`

  ### Audio — 오디오 재생/설정
  - 위치: `Assets/Script/GamePlay/Audio/` (AudioManager 등 구현/설정), `Audio/Interface/`(`Script.Audio.Interface`), `Audio/MonoBehaviour/`(AudioPlayer), `Audio/MonoBehaviour/Interface/`(IAudioPlayer) — AudioPooling/IAudioPooling은 UIPooling/StagePooling과 같은 성격이라 `Assets/Script/GamePlay/Pool/`에 위치
  - `AudioData`(재생 파라미터 묶음: key/type/loop/autoRelease/pitch/is3D)와 `AudioGroup`(Master/BGM/Effect/Voice)은 `Assets/Script/GameInfo/Audio/`의 기획 데이터(Unity/Server 공용) — 다른 `xxInfo`(예: `CharacterInfo.hitAudio`/`jumpAudio`)가 필드로 들고 있다가 `PlayAsync(AudioData, ...)`에 그대로 넘겨 재생
  - Unity `AudioMixer`(Local Addressable, key=`"AudioMixer"`) 기반
  - `Addressable`/`GameSetting`처럼 `StartUpLogic`이 `IAudioManager.InitializeAudioManager()`를 명시적으로 호출 후 `Initialized` 폴링
  - `PlayAsync(key, group, loop, autoRelease, pitch, is3D, position, track)` (또는 `PlayAsync(AudioData, position, track)`) — `loop=false`면 풀에서 `AudioPlayer`(AudioSource 보유)를 빌려 재생 완료 시 자동 반환(`AudioSource.PlayOneShot`은 인스턴스별 Stop이 불가해 미사용). `is3D=true`+`track` 지정 시 대상 Transform에 SetParent로 붙어 위치 추적(대상이 재생 중 Destroy되면 소리도 같이 끊김에 유의). 그룹 볼륨은 AudioMixer에서만 제어하므로 개별 소스 볼륨은 항상 최대(1). 그룹별 `AudioMaxCount` 초과 요청은 거부하지 않고 그룹 내 가장 오래 재생 중인 인스턴스를 강제 종료(Dequeue)해 자리를 내줌
  - `Stop(AudioHandle)` 외에 `Stop(string key)`/`Stop(AudioData)`도 지원 — 핸들 없이 key만 아는 호출부를 위한 편의 오버로드, 같은 key로 재생 중인 모든 인스턴스를 정지
  - `PlayBGM(key)`/`StopBGM()` — BGM 전용 AudioSource 1개로 별도 관리(위치 없음, 항상 loop, 풀/MaxCount 무관)
  - `AudioPlayer`(MonoBehaviour, `IPoolMember`)를 `AudioPlayerPrefab`으로 Addressable 등록해야 함(Unity Editor에서 수동 생성 필요) — `AudioPooling.cs`가 `UIPooling`/`StagePooling`과 동일한 `GameObjectPool` 기반 풀링 재사용
  - 클립은 `ReleaseClip`/`ReleaseAllClips` 호출 전까지 캐시 유지(`ScreenManager.ResourceClear()`와 동일 정책)
  - `AudioSetting`이 그룹별 볼륨/뮤트를 `IDataBase`로 로컬 저장(Json), 변경 시 즉시 저장
  - AudioListener는 이 모듈 범위 밖(카메라에 직접 부착)
  - 상세 내용: `Assets/Script/GamePlay/Audio/ARCHITECTURE.md`

  ### GameTimer — 전역 타이머
  - 위치: `Assets/Script/GameTimer/`
  - 앱 전역 시간 값 제공 (`Elapsed`, `DeltaTime`, `FixedElapsed`, `FixedTime`)
  - `Pause()` / `Resume()` — DeltaTime을 0으로 만들어 누적 중단 (일시정지 구현용)
  - UniTask 비동기 루프 2개 독립 실행 (Update / FixedUpdate 기준)
  - 상세 내용: `Assets/Script/GameTimer/README.md`

  ### Localize — 다국어 텍스트
  - 위치: `Assets/Script/Localize/`
  - Unity Localization 패키지 래퍼. 키 형식: `"TableName/EntryName"`
  - `GetErrorMessage(ErrorMessage)` — 열거형 기반 에러 메시지 타입 안전 조회
  - 현재 한국어(`"ko"`) 고정, 향후 시스템 언어 자동 감지 예정
  - `LocalizeText`: Inspector `[SerializeField]` 직렬화용 래퍼. 동기/비동기 조회 + 로케일 변경 이벤트 구독
  - 상세 내용: `Assets/Script/Localize/README.md`

  ### SceneLoader — 씬 전환
  - 위치: `Assets/Script/SceneLoader/`
  - Addressable Additive 로드 방식: UI 닫기 → 리소스 정리 → 새 씬 로드 → Active 설정 → 이전 씬 언로드
  - 상세 내용: `Assets/Script/SceneLoader/README.md`

  ### Utility — 공통 유틸리티
  - 위치: `Assets/Script/Utility/`
  - `Public/`: `ArrayUtility`(배열 확장), `ListPool`(List 풀링)
  - `Runtime/`: `ClassPool`(클래스 인스턴스 풀링, `IClassPool` OnRent/OnReturn 훅), `TransformUtility`(축별 Transform 설정), `ECSUtility`(NativeList 탐색/제거), `ExtensionUtility`(GameObject/Button 편의), `SpineUtility`(Spine 애니메이션 재생)
  - `Editor/`: `LocalizeUtility`(StringTable CRUD, Inspector 바인딩 — 에디터 전용)
  - 상세 내용: `Assets/Script/Utility/ARCHITECTURE.md`

  ## 작업 레시피

  ### 새 기획 데이터(Info) 추가
  1. `Assets/Script/GameInfo/Xxx/XxxInfo.cs` 생성
     - `InfoBase` 상속, `[System.Serializable]` 필수
     - 클래스명 = 파일명, `xxInfo` 접미사 필수
     - Unity 전용 패키지 사용 금지 (서버 공용)
  2. 테이블 자동 생성이 필요하면 클래스에 `[AutoEditorTable(true)]` 추가
  3. Unity 메뉴 → `Generator > GameInfo > 테이블 자동 생성` 실행
  4. 생성된 `XxxTable.asset`을 `Assets/GAME_INFO_TABLE/`에 배치

  ### 새 Inspector Attribute 추가
  1. `Assets/Script/GameInfo/Attribute/XxxAttribute.cs` — Attribute 클래스 선언
  2. `Assets/Script/Editor/Attribute/XxxDrawer.cs` — Odin PropertyDrawer 구현

  ### 새 Screen(UI) 추가
  1. `Assets/Script/GUI/` 하위에 Screen 클래스 작성 (`Screen` 베이스 상속)
  2. `ScreenData`에 Addressable AssetReference 등록
  3. 코드에서 참조 시 `[ScreenKey]` 어트리뷰트로 Inspector 드롭다운 사용
  4. 열기: `await _screenManager.OpenAsync(screenKey)`
  5. DontClose가 필요하면 `ScreenOption.DontClose` 설정

  ### 새 FSM Node 추가 (캐릭터 행동 상태)
  1. 기획 데이터: `Assets/Script/GameInfo/Character/Node/XxxNode.cs` — `NodeBase` 상속
  2. Client 구현: `Assets/Script/GamePlay/Character/Node/ClientXxxNode.cs` — `ClientNodeBase` 상속
     - `Enter()`, `Update()`, `End()` 오버라이드
     - `ClassPool` 재사용 대상이므로 `OnReturn()`에서 상태 초기화

  ### 새 FSM Transition 추가 (상태 전환 조건)
  1. 기획 데이터: `Assets/Script/GameInfo/Character/Transition/XxxTransition.cs` — `TransitionBase` 상속
  2. Client 구현: `Assets/Script/GamePlay/Character/Transition/ClientXxxTransition.cs` — `ClientTransitionBase` 상속
     - `OnTrigger()` 오버라이드 — 전환 조건 반환
     - `Priority` 설정 (높을수록 먼저 평가)

  ### 새 Stage Action 추가 (스테이지 이벤트)
  1. 기획 데이터: `Assets/Script/GameInfo/Dungeon/Action/XxxAction.cs` — `ActionBase` 상속
  2. Client 구현: `Assets/Script/GamePlay/Stage/Action/ClientXxxAction.cs` — `ClientActionBase` 상속
     - `ExecuteAsync()` 오버라이드
  3. Unity 메뉴 → `Generator > ActionFactory 재생성` 실행 (CodeGen 자동 갱신)

  ### 새 Stage Trigger 추가 (스테이지 종료 조건)
  1. 기획 데이터: `Assets/Script/GameInfo/Dungeon/Trigger/XxxTrigger.cs` — `TriggerBase` 상속
  2. Client 구현: `Assets/Script/GamePlay/Stage/Trigger/ClientXxxTrigger.cs` — `ClientTriggerBase` 상속
     - `OnTrigger()` 오버라이드
  3. Unity 메뉴 → `Generator > TriggerFactory 재생성` 실행 (CodeGen 자동 갱신)

  ### 새 IClient 요청 메서드 추가
  1. `Assets/Script/Client/Interface/IClient.cs`에 메서드 선언 추가
  2. `Assets/Script/Client/GameClient.Client.cs`에 로컬 DB 기반 구현 추가
  3. 서버 연동 시 이 구현체만 교체

  ### 새 LifetimeScope 서비스 등록
  - 어느 Scope에 등록할지 결정 (App / Client / Group / Stage)
  - 해당 `XxxLifetimeScope.cs`의 `Configure()`에 `builder.Register<Impl>().As<IInterface>().WithLifetime...` 추가
  - EntryPoint 필요 시 `RegisterEntryPoint<T>()` 사용

  ## Rules
  - 커밋 메시지는 한국어로 작성
  - PR 없이 main 브랜치에 직접 push 금지