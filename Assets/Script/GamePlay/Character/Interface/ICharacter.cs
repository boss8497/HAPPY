using Cysharp.Threading.Tasks;
using Script.GamePlay.Input;
using Script.GamePlay.Service.Interface;
using VContainer;

namespace Script.GamePlay.Character.Interface {
    public interface ICharacter {
        IPlayerControls PlayerControls { get; }
        IGroupService   GroupService   { get; }
        IObjectResolver Resolver       { get; }


        void    Initialize();
        void    Release();
        UniTask StartAsync();
    }
}