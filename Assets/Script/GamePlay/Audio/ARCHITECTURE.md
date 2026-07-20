# Audio — 오디오 재생/설정

Unity `AudioMixer`(Local Addressable, key=`"AudioMixer"`) 기반 오디오 재생/볼륨 관리 시스템.
`Assets/Settings/AudioMixer.mixer`에 Master/BGM/Effect/Voice 그룹이 정의되어 있고,
각 그룹의 Volume이 `MasterVolume`/`BGMVolume`/`EffectVolume`/`VoiceVolume`이라는 이름으로 Exposed Parameter에 노출되어 있어야
`AudioMixer.SetFloat()`으로 런타임 제어가 가능하다.

AudioListener는 이 모듈에서 다루지 않는다(메인 카메라에 직접 부착).

## ⚠️ 코드 완료 후 필수 수동 작업 — AudioPlayerPrefab

위치 기반(3D) 재생은 `AudioPlayer` 컴포넌트가 붙은 프리팹을 풀링해서 사용한다. 이 프리팹은 Unity Editor에서
직접 만들어야 한다(스크립트만으로는 신뢰성 있게 생성할 수 없어 의도적으로 비워둠):

1. 빈 GameObject 생성 → `AudioPlayer` 컴포넌트 추가(`[RequireComponent(typeof(AudioSource))]`라 `AudioSource`는 자동으로 붙음)
2. 3D 사운드 감쇠를 어떤 커브로 쓸지 등 `AudioSource`의 3D Sound Settings는 필요에 맞게 조정(코드에서는 `spatialBlend`만 재생 시점에 강제 설정함)
3. 프리팹으로 저장 후 Addressable로 등록, **Address(키)를 정확히 `AudioPlayerPrefab`으로 지정**
   (`Assets/Script/GamePlay/Pool/AudioPooling.cs`의 `AudioPlayerPrefabKey` 상수와 반드시 일치해야 함 — 이름을 바꾸고 싶다면 상수도 같이 바꿀 것)
4. `AudioMixer.mixer`와 마찬가지로 Local 그룹에 포함시켜 앱 시작 시 함께 받아지도록 구성

이 프리팹이 없으면 `PlayAsync()`가 처음 호출되는 시점(풀이 처음 생성되는 시점)에 로드가 실패한다.

## 폴더 구조

```
Assets/Script/GameInfo/Audio/
  (root)                 AudioData, AudioGroup — Unity/Server 공용 기획 데이터(Script.GameInfo 어셈블리)

Assets/Script/GamePlay/Audio/
  Interface/             Script.Audio.Interface.asmdef — IAudioManager, AudioHandle
                          (UniTask, Script.GameInfo 외 Unity 전용 의존성 없음 — 어디서든 참조 가능하도록 최소화)
  Data/                   AudioSetting.cs
  Data/Model/             AudioSettingModel.cs
  MonoBehaviour/          Script.Audio.MonoBehaviour.asmdef — AudioPlayer(MonoBehaviour)
  MonoBehaviour/Interface/ Script.Audio.MonoBehaviour.Interface.asmdef — IAudioPlayer
  (root)                  Script.Audio.asmdef — AudioManager 등 실제 구현

Assets/Script/GamePlay/Pool/
  (root)                  AudioPooling.cs — UIPooling/StagePooling과 같은 위치의 형제 풀 매니저
  Interface/               Script.GamePlay.Pool.Interface.asmdef — IStagePooling, IPoolMember, IAudioPooling 등
```

- **`AudioGroup`은 원래 `Script.Audio.Interface`의 `AudioGroupType`(Unity 전용 enum)이었으나, 다른 `xxInfo`가 사운드를
  참조할 수 있어야 해서 `Assets/Script/GameInfo/Audio/AudioGroup.cs`로 옮기고 이름도 `AudioGroup`으로 정리했다.**
  `Script.Audio.Interface`/`Script.Audio.MonoBehaviour`/`Script.Audio.MonoBehaviour.Interface`/`Script.Audio` 4개
  asmdef 모두 `Script.GameInfo`를 참조하도록 갱신됨.
- **`AudioData`**(`Assets/Script/GameInfo/Audio/AudioData.cs`) — `key`(`[AssetPath]`) + `type`(`AudioGroup`) +
  `loop`/`autoRelease`/`pitch`/`is3D`를 한 데 묶은 기획 데이터. `CharacterInfo.hitAudio`/`jumpAudio`처럼 다른
  `xxInfo`가 "이 상황에 이 소리를 이렇게 재생한다"를 필드 하나로 선언할 수 있게 하기 위함
  (`PlayAsync(AudioData, position, track, ct)` 오버로드로 그대로 재생).

`AudioPooling`/`IAudioPooling`은 Audio 모듈이 아니라 **Pool 모듈**에 있다 — UIPooling/StagePooling과 같은 성격의
풀 매니저라서(둘 다 `GameObjectPool` 기반) Audio 쪽에 두는 것보다 Pool 모듈의 형제로 두는 게 맞다고 판단해 이동함.
`IAudioPooling : IStagePooling`으로 상속받아 `GameObjectPool` 생성자가 요구하는 타입을 자동으로 만족시킨다
(`AudioPooling` 클래스 자체가 `IStagePooling`을 다시 선언할 필요가 없어짐).

## 부팅 순서

`Addressable`/`GameSetting`과 동일하게 `AppLifetimeScope`가 준비된 뒤 `StartUpLogic`이 명시적으로
`IAudioManager.InitializeAudioManager()`를 호출하고 `Initialized` 플래그를 폴링한다(`GameSetting.InitializeGameSetting()`과 동일 패턴).
`IInitializable.Initialize()`는 비워둔다 — Addressable/DataBase가 준비되기 전에 컨테이너 빌드 시점에서 자동 실행되면 안 되기 때문.

```
LoadMixerAsync()         — Addressables.LoadAssetAsync<AudioMixer>("AudioMixer"), 핸들은 앱 생존 기간 유지
CacheMixerGroups()       — FindMatchingGroups(string.Empty) 순회 후 그룹 이름으로 enum 매칭
CreateBgmSource()        — BGM 전용 AudioSource 1개를 별도 GameObject로 생성(DontDestroyOnLoad)
AudioSetting.LoadAsync() — DataBase.Initialized 대기 후 로드, 없으면 기본값 생성+저장
그룹별 ApplyVolume()     — 저장된 값을 AudioMixer에 반영
MonitorAsync() 시작      — 매 프레임 재생 종료된(loop=false) 플레이어 자동 반환 + 파괴된 플레이어 정리
```

`AudioPooling`(AudioPlayer 프리팹 풀)은 이와 별도로 `IInitializable.Initialize()`에서 루트 GameObject만
만들어두고, 실제 프리팹 로드는 `PlayAsync()`가 3D 사운드를 처음 요청할 때 지연 로드된다(`UIPooling`/`StagePooling`과 동일한 지연 전략).

## Play 정책

- `PlayAsync(key, group, loop, autoRelease, pitch, is3D, position, track)` — 플랫 optional 파라미터 스타일
  (`Addressable.DownloadDependenciesAsync`와 동일 관례, 별도 옵션 struct 없음)
- `PlayAsync(AudioData audioData, position, track, ct)` — `key`/`group`/`loop`/`autoRelease`/`pitch`/`is3D`를
  기획 데이터(`AudioData`) 하나로 받는 오버로드. 내부 로직은 위 `PlayAsync(key, ...)`와 완전히 동일하고
  각 필드를 그대로 대입해 호출한다(위치/추적만 호출부에서 별도로 넘김). `CharacterInfo.hitAudio`처럼 다른
  `xxInfo`가 들고 있는 `AudioData`를 그대로 넘겨 재생할 때 사용.
- `Stop(AudioHandle)` 외에 `Stop(string key)` / `Stop(AudioData audioData)`도 지원한다. 둘 다 핸들을 들고
  있지 않은 호출부(예: `AudioData`만 아는 다른 Info/System)를 위한 편의 오버로드로, 내부적으로
  `StopAllByClip(key)`를 그대로 재사용해 같은 key로 재생 중인 모든 인스턴스를 정지한다
  (`ReleaseClip(key)`가 이미 이 메서드로 정지 후 캐시를 해제하는 것과 동일한 방식). `Stop(AudioData)`는
  `audioData == null`이면 아무 동작도 하지 않는다.
- **BGM은 별도 메서드**: `PlayBGM(key)` / `StopBGM()`. 위치가 필요 없고 항상 그룹=BGM, 항상 loop이므로
  `PlayAsync`와 파라미터 조합을 나누는 대신 아예 메서드를 분리했다. BGM은 풀을 거치지 않는 전용 `AudioSource` 1개로
  관리되며(크로스페이드 없이 새 곡 재생 시 이전 곡은 즉시 정지), `AudioMaxCount` 풀 제한과 무관하다.
- **OneShot을 별도 파라미터로 두지 않은 이유**: "단발 재생이냐"는 `!loop`와 동치라 별도 플래그가 불필요하다.
  Unity `AudioSource.PlayOneShot()`은 인스턴스별 Stop이 불가능해 핸들 추적이 안 되므로,
  실제로는 풀에서 전용 `AudioPlayer`(AudioSource 보유)를 빌려 `source.clip = ...; source.Play()`로 재생한다.
  `loop == false`인 플레이어는 재생이 끝나면 `MonitorAsync`가 자동으로 풀에 반환하고,
  `loop == true`는 명시적으로 `Stop()`을 호출해야 반환된다.
- **위치/추적(`is3D`, `position`, `track`)**: `is3D=true`면 `AudioSource.spatialBlend=1`로 재생한다.
  `track`(Transform)이 있으면 `AudioPlayer.transform`을 그 자식으로 붙여(`SetParent`) 위치를 계속 따라가고,
  없으면 `position`(Vector3)에 스냅샷으로 배치한다. `is3D=false`면 둘 다 무시된다.
  **주의**: `track` 방식은 대상 GameObject가 재생 도중 `Destroy()`되면 자식인 `AudioPlayer`도 함께 파괴되어
  소리가 끊긴다(요청에 따라 단순 SetParent로 구현 — 짧게 끝나는 이펙트 사운드에만 사용 권장).
  `MonitorAsync`는 이렇게 파괴된 플레이어를 매 프레임 감지해 내부 장부(`_activeGroups`)에서 정리한다
  (풀에 반환할 오브젝트 자체가 사라졌으므로 Push는 하지 않고 카운트만 되돌림 — 해당 풀 슬롯은 사실상 소실됨).
- **볼륨은 그룹(AudioMixer)에서만 제어**: 그룹 볼륨이 `SetFloat`으로 dB 적용되므로 개별 `AudioSource.volume`은
  항상 `1`(최대)로 고정한다. 개별 인스턴스 볼륨 스케일 파라미터는 두지 않았다 — 필요해지면(예: 거리 감쇠) 그때 추가한다.
- **같은 key 재생 요청 처리(`FindActiveByClipKey`)**: `PlayAsync` 진입 시 가장 먼저 같은 key가 이미 재생 중인지
  확인한다. 있으면 새 플레이어를 빌리지 않고 그 인스턴스를 그대로 재사용해 `source.time = 0`으로 처음부터 다시
  재생한다(그룹/loop/pitch/3D 설정은 이번 호출 값으로 갱신). 새 슬롯을 점유하지 않으므로 그룹이 `AudioMaxCount`에
  걸려 있어도 이 재생 요청은 항상 통과한다. BGM은 `_activePlayers`에 들어오지 않으므로 이 로직의 대상이 아니다.
- **AudioMaxCount(그룹별 동시 재생 제한) — 초과 시 Dequeue**: `AudioManager.MaxConcurrent`에 그룹별 상한을 정의한다
  (기본값: Master 4 / Effect 8 / Voice 3 — BGM은 전용 소스라 대상 아님). 상한을 넘으면(그리고 같은 key 재생 요청이
  아니면) 요청을 거부하는 대신 `FindOldestInGroup`으로 그 그룹에서 가장 오래 재생 중인 플레이어를 찾아 강제
  종료(`ReturnPlayer`)하고 새 사운드에게 자리를 내준다 — 대여 순번(`_rentOrder`, 대여할 때마다 증가하는
  시퀀스 번호)이 가장 작은 것이 대상. `Replay()`로 다시 재생된 플레이어는 순번이 갱신되어 곧바로 다시
  Dequeue 대상이 되지 않는다. 그룹에 살아있는 플레이어가 하나도 없는 예외적인 경우에만
  `AudioHandle.Invalid`를 반환한다. 체크는 클립 로드가 끝난 뒤, 실제로 풀 슬롯을 점유하기 직전에 수행한다 —
  이미 캐시된 클립이면 체크가 거의 즉시 이뤄지지만, 처음 로드하는 클립은 로드 완료 시점까지 상한 체크가
  지연되므로 이론상 동시에 여러 요청이 몰리면 짧은 순간 상한을 넘길 수 있다(엄격한 실시간 보장이 필요한 값은
  아니라고 판단해 감수함).
- **autoRelease 주의사항**: `loop == false && autoRelease`면 `Play()` 호출 직후 Addressable 핸들을 바로 Release한다(요청 사양).
  `loop == true`(BGM 등)는 재생 도중 캐시를 지우면 안 되므로 autoRelease를 무시한다.
  `AudioSource.clip`이 이미 로드된 `AudioClip` 객체를 참조 중이므로 대부분의 경우 재생은 문제없이 이어지지만,
  Addressable 설정이 스트리밍/압축 해제 지연 로드인 클립이라면 재생 도중 언로드될 위험이 이론상 존재한다.
  자주 겹쳐 재생되는 SFX처럼 위험을 감수할 만한 케이스에만 `autoRelease=true`를 쓰고,
  자주 재사용되는 오디오는 `autoRelease=false`로 캐시에 남겨 로드 비용을 줄인다.
- 캐시(`_loadedClips`)는 명시적으로 `ReleaseClip(key)` / `ReleaseAllClips()`를 호출하기 전까지 유지된다
  (`ScreenManager.ResourceClear()`와 동일한 정책 — 씬 전환 등 필요한 시점에 호출).

## 풀링 — AudioPlayer / AudioPooling

- `AudioPlayer`(`MonoBehaviour`, `[RequireComponent(typeof(AudioSource))]`) — 실제 재생 단위.
  `IAudioPlayer`(공개 계약)와 `IPoolMember`(풀 인프라 연동)를 함께 구현한다.
  `IPoolMember` 구현체라 `GameObjectPool`/`UIPooling`/`StagePooling`이 쓰는 풀링 인프라를 그대로 재사용한다.
- `AudioPooling`(`Assets/Script/GamePlay/Pool/AudioPooling.cs`)은 `UIPooling`/`StagePooling`과 동일한 구조
  (`GameObjectPool` + `IPoolMember` 기반)를 그대로 따른다. `IAudioPooling : IStagePooling`이라 `GameObjectPool`
  생성자가 요구하는 타입을 만족시키면서도, `AudioManager`가 실제로 의존하는 건 좁은 `IAudioPooling`
  (`Pop(Transform): IAudioPlayer` / `Push(IAudioPlayer)`)뿐이다.
- `AudioPooling`은 `UIPooling`처럼 App scope에 등록되므로 `Resolver`는 `IScopeLocator.GetLastChildScope()`를
  통해 그때그때 가장 최근 자식 Scope의 Container를 참조한다(App scope 시점엔 아직 자식 Scope가 없을 수 있음 — 실제
  3D 사운드 재생은 게임 진행 이후에나 호출되므로 문제되지 않는다는 전제는 UIPooling과 동일).

## Handle / Generation / IsAlive

- `AudioHandle { InstanceId, Generation, IsValid }` — `InstanceId`는 `AudioPlayer.GetInstanceID()`.
  `AudioPlayer.ReturnToIdle()`마다 `Generation`이 증가해 이미 반환된(재사용된) 인스턴스를 가리키는 오래된 핸들로
  `Stop()`을 호출해도 무시된다(`AudioPlayer.Matches()`). `IsValid`는 별도 플래그로 관리한다
  (Unity InstanceId는 음수일 수 있어 부호로 유효성을 판별할 수 없음).
- **`IAudioPlayer.IsAlive` 필수 사용 규칙**: `_activePlayers`는 `Dictionary<int, IAudioPlayer>`(인터페이스 타입)이라
  `player == null`/`player != null` 비교는 Unity의 "destroyed 오브젝트 == null" 오버로드가 적용되지 않는다
  (그 오버로드는 `UnityEngine.Object` 및 하위 concrete 타입으로 비교할 때만 걸림 — 인터페이스 타입으로는 항상
  false, 즉 "살아있음"으로 나온다). `track()`으로 붙은 대상이 재생 도중 Destroy되면 `AudioPlayer`도 함께 파괴되는데,
  이걸 감지 못 하면 `player.Source.isPlaying` 등 네이티브 호출에서 `MissingReferenceException`이 터지고
  `MonitorAsync`(`.Forget()`으로 도는 무한 루프)가 그 순간 영구히 죽어버린다. 그래서 `AudioPlayer`는
  `IsAlive => this != null`(여기서는 `this`의 정적 타입이 `AudioPlayer:MonoBehaviour`라 오버로드가 정상 동작)을
  구현해 인터페이스로 안전하게 노출하고, `AudioManager.Play.cs`의 모든 생존 체크는 `player.IsAlive`를 사용한다
  (`player == null` 절대 사용 금지).

## 볼륨/뮤트

- `AudioSetting`이 `AudioSettingModel`(`[MessagePackObject]`, 현재는 `DataType.Json`으로 저장 — `GroupModel` 선례와 동일하게
  추후 MessagePack 전환을 대비해 Key 어트리뷰트만 미리 부여)을 `IDataBase`로 로드/저장한다.
- `SetVolume`/`SetMute` 호출 시 즉시 AudioMixer에 반영 + 즉시 `SaveAsync()`(요청 사양 — 잦은 저장을 감수).
- Mute는 별도 AudioMixer API가 아니라 `SetFloat(paramName, -80dB)`로 구현한다(그룹 Mute는 에디터 프리뷰 전용이라 런타임 제어 불가).

## 연관 경로

- Mixer 에셋: `Assets/Settings/AudioMixer.mixer` (Exposed Parameters 4개 필수)
- AudioPlayer 프리팹: `Assets/Script/GamePlay/Pool/AudioPooling.cs`의 `AudioPlayerPrefabKey`("AudioPlayerPrefab")로
  Addressable 등록 필요(위 수동 작업 참고)
- 풀 매니저: `Assets/Script/GamePlay/Pool/AudioPooling.cs`, `Assets/Script/GamePlay/Pool/Interface/IAudioPooling.cs`
- 부팅 호출부: `Assets/Script/Scene/StartUpLogic.cs`
- DI 등록: `Assets/Script/LifetimeScope/AppLifetimeScope.cs`

## 향후 확장 후보 (미구현)

- BGM 크로스페이드/DOTween 연동(현재는 이전 곡을 즉시 Stop 후 새 곡 재생)
- `[ScreenKey]`처럼 Inspector 드롭다운으로 오디오 Addressable key를 선택하는 `[AudioKey]` Attribute + Drawer
- 거리 감쇠 외 인스턴스별 볼륨 스케일이 필요해질 경우 `PlayAsync`에 파라미터 추가 검토
