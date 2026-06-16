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

  ## Rules
  - 커밋 메시지는 한국어로 작성
  - PR 없이 main 브랜치에 직접 push 금지