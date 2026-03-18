using Script.GamePlay.Input;

namespace Script.GamePlay.Character.Interface {
    public interface ICharacter {
        
        IPlayerControls PlayerControls { get; }
        
        
        void Initialize();
        void Release();
        void Start();
    }
}