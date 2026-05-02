using Cysharp.Threading.Tasks;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;

namespace Script.GamePlay.Service.Interface {
    public interface IGroupService : IService {
        IGroupData GroupData { get; }

        DungeonProgress GetDungeon(Category     dungeonCategory);
        UniTask         ClearedDungeon(Category dungeonCategory);
    }
}