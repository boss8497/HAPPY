using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Script.GameInfo.Character;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    [Serializable]
    public class ClientDieNode : ClientNodeBase, IClassPool {
        private Character    _character;
        private DieAnimation _dieAnimation;

        public override ClientNodeBase Initialize(CharacterBehaviour characterBehaviour, NodeBase nodeBase) {
            base.Initialize(characterBehaviour, nodeBase);
            _character    = characterBehaviour.Character;
            _dieAnimation = _character.DieAnimation;
            if (_dieAnimation != null) {
                _dieAnimation.ResetAnimation().Forget();
            }
            return this;
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
            
            if(_character.IsPlayer == false) {
                _character.StageManager.AddRemoveEnemy(_character);
            }
        }

        public void OnRent() {
        }
        public void OnReturn() {
        }
    }
}