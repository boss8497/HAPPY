using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Pool;
using Script.GamePlay.Service.Interface;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Stage {
    public interface IStageManager {
        //Injection
        IGroupService   Group        { get; }
        IObjectResolver Resolver     { get; }
        IStagePooling   StagePooling { get; }


        //Reactive
        ReactiveProperty<StageState>   State         { get; }
        ReadOnlyReactiveProperty<bool> Initialized   { get; }
        ReadOnlyReactiveProperty<bool> SystemControl { get; }
        ReadOnlyReactiveProperty<bool> Fail          { get; }
        ReadOnlyReactiveProperty<bool> Clear         { get; }
        ReadOnlyReactiveProperty<bool> NextPhase     { get; }


        ReadOnlyReactiveProperty<DungeonInfo>            DungeonInfo { get; }
        ReadOnlyReactiveProperty<GameInfo.Dungeon.Stage> Stage       { get; }

        //GamePlay
        List<Character.Character> Players { get; }
        List<Character.Character> Enemies { get; }

        void    Initialize(DungeonProgress dungeonProgress);
        UniTask Begin();
        UniTask Start();
        UniTask End();
        UniTask ReStart();
        void    Release();


        void AddCharacter(GameObject obj);
        void AddEnemy(GameObject     obj);
    }
}