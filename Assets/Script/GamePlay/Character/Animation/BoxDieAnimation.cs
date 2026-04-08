using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Script.GamePlay.Character {
    
    public class BoxDieAnimation : DieAnimation {
        [SerializeField]
        private SpriteRenderer spriteRenderer;
        
        public override async UniTask PlayAnimation() {
            var tween = spriteRenderer.DOFade(0, 1f).SetEase(Ease.InOutSine);
            await tween.AsyncWaitForCompletion();
        }

        public override UniTask ResetAnimation() {
            spriteRenderer.color =  new (spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f); 
            return UniTask.CompletedTask;
        }
    }
}