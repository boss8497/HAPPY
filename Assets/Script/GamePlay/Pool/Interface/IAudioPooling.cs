using Script.GamePlay.Pool;
using UnityEngine;

namespace Script.GamePlay.Audio {
    /// <summary>
    /// AudioManager가 사용하는 좁은 퍼사드. 실제 풀링 로직은 AudioPooling(IStagePooling 구조 재사용)에 있다.
    /// </summary>
    public interface IAudioPooling : IStagePooling {
        IAudioPlayer Pop(Transform     parent = null);
        void         Push(IAudioPlayer player);
    }
}
