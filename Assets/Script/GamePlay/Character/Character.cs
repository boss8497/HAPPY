using System.Linq;
using Script.GamePlay.Character.Interface;
using Script.Utility.Runtime;
using UnityEngine;

namespace Script.GamePlay.Character {
    public partial class Character : MonoBehaviour, ICharacter {


        //Test를 위해 OnEnable에 Character 실행 코드 다 넣어둠
        private void OnEnable() {
            Initialize();
            Start();
        }


        #region Interface
        //캐릭터 소환시 꼭 실행
        public void Initialize() {
            InitializeGamePlay();
            
            // Reactive 초기화, Reactive는 제일 마지막에 초기화가 좋음
            InitializeReactiveProperty();
        }
        
        public void Release() {
            ReleaseGamePlay();
            ReleaseReactiveProperty();
        }

        public void Start() {
            //FSM 실행
            _characterBehaviour.Start();
        }

        #endregion

        public float SetAnimation(string animationName, bool loop = false, bool hasExit = false) {
            if (SkeletonAnimation == null) {
                Debug.LogError($"SkeletonAnimation is null");
                return 0f;
            }

            if ((SpineAnimationNames?.Any(a => a == animationName) ?? true) == false) {
                return 0f;
            }

            //애니메이션이 같으면
            var currentAnimation = SkeletonAnimation.AnimationState.GetCurrent(0);
            if (currentAnimation != null && animationName.Equals(currentAnimation.Animation.Name) && currentAnimation.Loop == loop) {
                return 0;
            }

            return SkeletonAnimation.StartAnimation(animationName, loop, hasExit)?.AnimationEnd ?? 0;
        }
        
    }
}