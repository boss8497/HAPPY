using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameData.Model;
using Script.GameInfo.Info;
using UnityEngine;

namespace Script.GamePlay.Audio.Interface {
    public interface IAudioManager {
        bool              Initialized       { get; }
        AudioSettingModel AudioSettingModel { get; }

        /// <summary>
        /// StartUpLogic에서 명시적으로 호출 — Addressable/DataBase가 준비된 이후 진행되어야 하기 때문에
        /// IInitializable.Initialize() 자동 실행이 아닌 IGameSetting과 동일한 명시 호출 패턴을 따른다.
        /// </summary>
        void InitializeAudioManager();

        /// <summary>
        /// 오디오 재생. loop=false면 재생이 끝나는 즉시 자동으로 풀에 반환된다(= 단발 재생).
        /// loop=true면 명시적으로 Stop()을 호출해야 반환된다. BGM은 이 메서드 대신 PlayBGM을 사용한다.
        /// loop=false && autoRelease=true면 재생 시작 직후 Addressable 핸들을 즉시 Release한다
        /// (loop=true일 때는 재생 중 캐시를 지우면 안 되므로 autoRelease를 무시한다).
        /// 그룹 볼륨은 AudioMixer에서 dB로 이미 적용되므로 AudioSource 자체 볼륨은 항상 최대(1)로 재생한다.
        /// 같은 key가 이미 재생 중이면 새로 빌리지 않고 그 인스턴스를 재사용해 처음부터 다시 재생한다
        /// (새 슬롯을 점유하지 않으므로 그룹이 상한이어도 이 재생 요청은 통과됨).
        /// 그룹별 동시 재생 개수 제한(AudioMaxCount)을 넘으면, 거부하는 대신 해당 그룹에서 가장 오래
        /// 재생 중인 플레이어를 강제 종료하고 자리를 내준다(BGM은 이 풀을 거치지 않으므로 대상이 될 수 없음).
        /// 대상이 하나도 없는 예외적인 경우에만 AudioHandle.Invalid를 반환한다.
        /// is3D=true면 AudioSource.spatialBlend=1로 재생하며, track이 지정되면 해당 Transform의 자식으로 붙어
        /// 위치를 계속 따라가고(대상이 재생 도중 Destroy되면 소리도 함께 끊김에 유의), track이 없으면 position에
        /// 스냅샷으로 배치한다. is3D=false면 position/track은 무시된다.
        /// </summary>
        UniTask<AudioHandle> PlayAsync(
            string            key,
            AudioGroup        group       = AudioGroup.Effect,
            bool              loop        = false,
            bool              autoRelease = true,
            float             pitch       = 1f,
            bool              is3D        = false,
            Vector3?          position    = null,
            Transform         track       = null,
            CancellationToken ct          = default
        );

        UniTask<AudioHandle> PlayAsync(
            AudioData         audioData,
            Vector3?          position = null,
            Transform         track    = null,
            CancellationToken ct       = default
        );

        void Stop(AudioHandle handle);

        /// <summary>
        /// 같은 key로 재생 중인 모든 인스턴스를 정지한다(핸들 없이 key만 아는 호출부를 위한 편의 메서드).
        /// </summary>
        void Stop(string key);

        /// <summary>
        /// AudioData.key 기준으로 재생 중인 모든 인스턴스를 정지한다. audioData가 null이면 아무 동작도 하지 않는다.
        /// </summary>
        void Stop(AudioData audioData);

        void StopAll(AudioGroup group);

        /// <summary>
        /// BGM 전용 재생 — 위치 개념이 없고 항상 그룹은 BGM, 항상 loop. 전용 AudioSource 1개로 관리되며
        /// AudioMaxCount 풀 제한과는 무관하다. 새로 호출하면 이전 BGM은 즉시 정지된다.
        /// </summary>
        UniTask PlayBGM(string key, CancellationToken ct = default);

        void StopBGM();

        float GetVolume(AudioGroup group);
        void  SetVolume(AudioGroup group, float volume01, bool save = true);
        bool  GetMute(AudioGroup   group);
        void  SetMute(AudioGroup   group, bool mute, bool save = true);

        /// <summary>
        /// 캐시된 특정 클립을 해제한다. 재생 중이면 먼저 정지시킨다.
        /// </summary>
        void ReleaseClip(string key);

        /// <summary>
        /// ScreenManager.ResourceClear()와 동일한 패턴 — 캐시된 모든 클립을 일괄 해제.
        /// </summary>
        void ReleaseAllClips();
    }
}