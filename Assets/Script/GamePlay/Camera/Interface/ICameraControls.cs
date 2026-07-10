namespace Script.GamePlay.Camera {
    public interface ICameraControls {
        UnityEngine.Camera MainCamera   { get; }
        float              OutSideLeftX { get; }
        float              InSideLeftX  { get; }
        float              SpawnOffset  { get; }

        /// <summary>
        /// 카메라를 흔든다. 연속 호출 시 Amplitude는 누적되며(최대 maxAmplitude), 흔들리는 시간은 정해진 duration으로 매번 재시작한다.
        /// amplitude가 0 이하면 아무 동작도 하지 않는다.
        /// </summary>
        void Shake(float amplitude);
    }
}