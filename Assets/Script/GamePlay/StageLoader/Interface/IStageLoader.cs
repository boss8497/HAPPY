using Cysharp.Threading.Tasks;

namespace Script.GamePlay.Stage {
    public interface IStageLoader {
        UniTask LoadStage(GameInfo.Dungeon.Stage stage);
    }
}