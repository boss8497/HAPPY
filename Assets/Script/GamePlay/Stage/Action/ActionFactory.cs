using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Stage {
    public static partial class ActionFactory {

        public static ClientActionBase Create(ActionBase action) {
            return CreateInternal(action);
        }
    }
}