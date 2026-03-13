using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Character;
using Script.GameInfo.Table;
using Script.GamePlay.Character.Interface;
using Script.Utility.Runtime;
using Spine.Unity;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public class Character : MonoBehaviour, ICharacter {
        #region GameInfo

        [Character]
        public int characterInfoUid;


        private CharacterInfo _characterInfo;

        public CharacterInfo CharacterCharacterInfo {
            get {
                if (InfoBase.ValidUid(characterInfoUid) == false) {
                    return null;
                }

                if (_characterInfo == null || _characterInfo.UID != characterInfoUid) {
                    _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(characterInfoUid);
                }

                return _characterInfo;
            }
        }

        private BehaviourInfo _behaviourInfo;

        public BehaviourInfo BehaviourInfo {
            get {
                if (CharacterCharacterInfo == null) return null;
                if (InfoBase.ValidUid(CharacterCharacterInfo.behaviourId) == false) {
                    return null;
                }

                if (_behaviourInfo == null || _behaviourInfo.UID != CharacterCharacterInfo.behaviourId) {
                    _behaviourInfo = GameInfoManager.Instance.Get<BehaviourInfo>(CharacterCharacterInfo.behaviourId);
                }

                return _behaviourInfo;
            }
        }

        #endregion


        #region GamePlay

        private CharacterBehaviour _characterBehaviour;
        public  CharacterBehaviour CharacterBehaviour => _characterBehaviour;


        [SerializeField]
        private SkeletonAnimation _skeletonAnimation;

        public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;

        private string[] _animationNames;
        public string[] SpineAnimationNames {
            get {
                _animationNames ??= SkeletonAnimation?.Skeleton.Data.Animations.Select(s => s.Name).ToArray() ?? Array.Empty<string>();
                return _animationNames;
            }
            set => _animationNames = value;
        }

        #endregion


        //Test를 위해 OnEnable에 Init
        private void OnEnable() {
            Initialize();
        }


        #region Interface

        public void Initialize() {
            _characterBehaviour ??= ClassPool.Get<CharacterBehaviour>();
            _characterBehaviour.Initialize(BehaviourInfo, this);

            //Spine 컴포넌트
            if (_skeletonAnimation == null) {
                _skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (_skeletonAnimation == null) {
                    _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
                }
            }
        }

        /// <summary> 오브젝트가 파괴 됐을때 호출 </summary>
        public void Release() {
            if (_characterBehaviour != null) {
                ClassPool.Release<CharacterBehaviour>(_characterBehaviour);
                _characterBehaviour = null;
            }
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
            if (animationName.Equals(currentAnimation.Animation.Name) && currentAnimation.Loop == loop) {
                return 0;
            }

            return SkeletonAnimation.StartAnimation(animationName, loop, hasExit)?.AnimationEnd ?? 0;
        }


        //end class
    }
}