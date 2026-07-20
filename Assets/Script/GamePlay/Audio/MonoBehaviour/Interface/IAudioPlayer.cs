using Script.GameInfo.Info;
using Script.GamePlay.Audio.Interface;
using UnityEngine;

namespace Script.GamePlay.Audio {
    public interface IAudioPlayer {
        public GameObject GameObject { get; }

        /// <summary>
        /// 네이티브 오브젝트가 아직 살아있는지(Destroy되지 않았는지) 여부.
        /// 인터페이스 타입(IAudioPlayer)으로는 Unity의 "destroyed == null" 오버로드가 적용되지 않으므로
        /// player == null 대신 반드시 이 프로퍼티로 확인해야 한다.
        /// </summary>
        bool IsAlive { get; }

        AudioSource    Source     { get; }
        int            Generation { get; }
        bool           Busy       { get; }
        string         ClipKey    { get; }
        bool           Loop       { get; }
        AudioGroup Group      { get; }
        string         Key        { get; }

        AudioHandle Rent(AudioGroup group, string clipKey, bool loop);
        void        ReturnToIdle();
        bool        Matches(AudioHandle handle);
        void        Track(Transform     target);
        void        SetPosition(Vector3 position);
        int         GetInstance();
    }
}