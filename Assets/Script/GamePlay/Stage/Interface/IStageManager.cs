using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameData.Model;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Camera;
using Script.GamePlay.Character;
using Script.GamePlay.Pool;
using Script.GamePlay.Service.Interface;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Stage {
    public interface IStageManager {
        //Injection
        IGroupService   Group          { get; }
        IObjectResolver Resolver       { get; }
        IStagePooling   StagePooling   { get; }
        ICameraControls CameraControls { get; }


        //Reactive
        ReactiveProperty<DungeonInfo>            DungeonInfo   { get; }
        ReactiveProperty<GameInfo.Dungeon.Stage> Stage         { get; }
        ReactiveProperty<StageState>             State         { get; }
        ReactiveProperty<float>                  Score         { get; }
        ReactiveProperty<float>                  RunningScore  { get; }
        ReactiveProperty<float>                  ItemScore     { get; }
        ReadOnlyReactiveProperty<bool>           Initialized   { get; }
        ReadOnlyReactiveProperty<bool>           SystemControl { get; }
        ReadOnlyReactiveProperty<bool>           Fail          { get; }
        ReadOnlyReactiveProperty<bool>           Clear         { get; }
        ReadOnlyReactiveProperty<bool>           NextPhase     { get; }
        ReadOnlyReactiveProperty<bool>           ReStartState  { get; }


        //GamePlay
        List<Character.ICharacter> Players { get; }
        List<Character.ICharacter> Enemies { get; }

        void    Initialize(DungeonInfo dungeonInfo, GameInfo.Dungeon.Stage stage);
        UniTask Begin();
        UniTask Start();
        UniTask End();
        UniTask ReStart();
        void    Release();
        
        bool AddCharacter(GameObject    obj);
        bool AddCharacter(ICharacter    character);
        bool AddEnemy(GameObject        obj);
        bool AddEnemy(ICharacter        character);


        void AddRemoveEnemy(ICharacter enemy);
        void AddItemScore(float         score);

        float GroundY { get; }
        void  SetGroundY(float groundY);

        void ResetState();
        void AddState(StageState    state);
        void RemoveState(StageState state);
        void Pause();
        void Resume();
    }
}