using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public abstract class ClientTriggerBase {
        private readonly TriggerBase _trigger;
        public           TriggerType Type => _trigger.type;

        public ClientTriggerBase(TriggerBase trigger) {
            _trigger = trigger;
        }


        public abstract void Initialize(IStageManager stageManager);
        public abstract bool OnTrigger();
        public abstract void Release();
    }
}