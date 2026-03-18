using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Info.Character;
using Script.GamePlay.Input;

namespace Script.GamePlay.Character {
    public class ClientPlayerControlNode : ClientNodeBase {
        private IPlayerControls  _controls;

        
        

        public ClientPlayerControlNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) {
            _controls = characterBehaviour?.Character?.PlayerControls;
        }

        public override void Initialize() {
            
        }

        protected override async UniTask Update(CancellationToken cts) {
            while (!cts.IsCancellationRequested) {


                
                //플레이어 조작을 컨트롤하기 때문에 DelayFrame을 사용하지 않고 Yield를 사용
                await UniTask.Yield();
            }
        }
    }
}