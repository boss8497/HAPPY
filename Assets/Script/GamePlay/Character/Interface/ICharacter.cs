using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Character;
using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using VContainer;

namespace Script.GamePlay.Character.Interface {
    public interface ICharacter {
        ReactiveProperty<CharacterState> State         { get; }
        ReactiveProperty<double>         Health        { get; }
        ReactiveProperty<double>         MaxHealth     { get; }
        ReadOnlyReactiveProperty<bool>   Initialized   { get; }
        ReadOnlyReactiveProperty<bool>   Jumping       { get; }
        ReadOnlyReactiveProperty<bool>   Running       { get; }
        ReadOnlyReactiveProperty<bool>   Die           { get; }
        ReadOnlyReactiveProperty<bool>   SystemControl { get; }


        IPlayerControls PlayerControls { get; }
        IGroupService   GroupService   { get; }
        IObjectResolver Resolver       { get; }


        void    Initialize(int team, bool isPlayer = false);
        void    Release();
        UniTask StartAsync();
    }
}