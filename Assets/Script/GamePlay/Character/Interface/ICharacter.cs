using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Character;
using Script.Buff;
using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using Script.GameTimer;
using UnityEngine;
using VContainer;

namespace Script.GamePlay.Character {
    public interface ICharacter {
        IGameTimer GameTimer { get; }
        
        
        ReactiveProperty<CharacterState> State          { get; }
        ReactiveProperty<double>         Health         { get; }
        ReactiveProperty<double>         MaxHealth      { get; }
        ReadOnlyReactiveProperty<bool>   Initialized    { get; }
        ReadOnlyReactiveProperty<bool>   Jumping        { get; }
        ReadOnlyReactiveProperty<bool>   Running        { get; }
        ReadOnlyReactiveProperty<bool>   Die            { get; }
        ReadOnlyReactiveProperty<bool>   SystemControl  { get; }
        ReadOnlyReactiveProperty<bool>   CollisionState { get; }


        IPlayerControls PlayerControls { get; }
        IGroupService   GroupService   { get; }
        IObjectResolver Resolver       { get; }

        Transform  Transform  { get; }
        GameObject GameObject { get; }


        void    Initialize(int team, bool isPlayer = false);
        void    Release();
        UniTask StartAsync();

        void SetState(CharacterState    state);
        void AddState(CharacterState    state, bool notify = true);
        void RemoveState(CharacterState state, bool notify = true);

        float GetCollisionDelayTime();
    }
}