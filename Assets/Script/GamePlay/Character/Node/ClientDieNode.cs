using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using UnityEngine;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientDieNode : ClientNodeBase {
        private Character    _character;
        private DieAnimation _dieAnimation;

        public ClientDieNode(CharacterBehaviour characterBehaviour, NodeBase nodeBase) : base(characterBehaviour, nodeBase) {
            _character    = characterBehaviour.Character;
            _dieAnimation = _character.DieAnimation;
        }

        public override void Initialize() {
            if (_dieAnimation != null) {
                _dieAnimation.ResetAnimation().Forget();
            }
        }

        protected override void Enter() {
            _character.RemoveState(CharacterState.Running);
        }

        protected override async UniTask Update(CancellationToken cts) {
            Debug.LogError($"죽었어!!! {CharacterBehaviour.Character.CharacterInfo.Name}");
            
            var dieAnimationTime = _characterBehaviour.Character.SetAnimation(nameof(AnimationName.DIE), false);
            await UniTask.WaitForSeconds(dieAnimationTime, cancellationToken: cts);
            
            if (_dieAnimation != null) {
                await _dieAnimation.PlayAnimation();
            }
        }
    }
}