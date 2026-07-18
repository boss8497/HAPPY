namespace Script.GamePlay.Audio.Interface {
    /// <summary>
    /// 재생 중인 AudioPlayer 인스턴스를 가리키는 핸들(GetInstanceID() 기반).
    /// Generation은 AudioPlayer가 풀로 반환/재사용될 때마다 증가 — Stop 호출 시
    /// 이미 반환된 인스턴스를 잘못 제어하는 것을 막는다.
    /// Unity InstanceId는 음수일 수도 있으므로 유효성은 별도 플래그로 관리한다.
    /// </summary>
    public readonly struct AudioHandle {
        public static readonly AudioHandle Invalid = default;

        public readonly int  InstanceId;
        public readonly int  Generation;
        public readonly bool IsValid;

        public AudioHandle(int instanceId, int generation) {
            InstanceId = instanceId;
            Generation = generation;
            IsValid    = true;
        }
    }
}
