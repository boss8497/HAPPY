using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Service.Interface;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Stage {
    public interface IStageManager {
        //Injection
        IGroupService   Group    { get; }
        IObjectResolver Resolver { get; }


        //Reactive
        ReactiveProperty<StageState>                     State         { get; }
        ReadOnlyReactiveProperty<bool>                   Initialized   { get; }
        ReadOnlyReactiveProperty<bool>                   SystemControl { get; }
        ReadOnlyReactiveProperty<DungeonInfo>            DungeonInfo   { get; }
        ReadOnlyReactiveProperty<GameInfo.Dungeon.Stage> Stage         { get; }

        void    Initialize(DungeonProgressModel dungeonProgressModel);
        UniTask Begin();
        UniTask Start();
        UniTask End();
        void    Release();
        
        
        //TestCode
        void AddCharacter(GameObject obj);
    }
}