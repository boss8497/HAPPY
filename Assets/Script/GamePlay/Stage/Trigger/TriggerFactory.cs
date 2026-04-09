using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public static partial class TriggerFactory {
        
        public static ClientTriggerBase Create(TriggerBase action) {
            return CreateInternal(action);
        }
    }
}