using System;
using System.Linq;
using Expression;
using R3;
using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Info.Character;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Character.Interface;
using Script.GamePlay.Input;
using Script.GamePlay.Stat;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;
using VContainer;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public class Character : MonoBehaviour, ICharacter {
        //<summary>
        // Inject
        //</summary>

        #region Injection

        private IPlayerControls _playerControls;

        [Inject]
        public void Constructor(
            IPlayerControls playerControls
        ) {
            _playerControls = playerControls;
        }


        public IPlayerControls PlayerControls => _playerControls;

        #endregion


        //<summary>
        // GameInfo
        //</summary>

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

        [SerializeField]
        private CharacterBehaviour _characterBehaviour;

        public CharacterBehaviour CharacterBehaviour => _characterBehaviour;


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


        [ShowInInspector]
        private Status _status;

        public Status Status => _status;

        #endregion


        #region GamePlayReactiveProperty

        public ReactiveProperty<double> Health { get; set; } = new();


        public ReadOnlyReactiveProperty<bool> IsDie { get; private set; }

        
        private DisposableBag _reactiveDisposableBag;
        #endregion


        //Test를 위해 OnEnable에 Init
        private void OnEnable() {
            Initialize();
        }


        #region Interface

        //캐릭터 소환시 꼭 실행
        public void Initialize() {
            _characterBehaviour ??= ClassPool.Get<CharacterBehaviour>();
            _characterBehaviour.Initialize(BehaviourInfo, this);

            _status ??= ClassPool.Get<Status>();

            //Spine 컴포넌트
            if (_skeletonAnimation == null) {
                _skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (_skeletonAnimation == null) {
                    _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
                }
            }

            //스텟 초기화
            InitializeStatus();

            //FSM 실행
            _characterBehaviour.Start();
        }

        private void InitializeStatus() {
            using var _ = CreateValueContext();
            foreach (var statusUid in _characterInfo.statusUids) {
                _status.Add(GameInfoManager.Instance.Get<StatusInfo>(statusUid));
            }
        }

        private void InitializeReactiveProperty() {
            _reactiveDisposableBag.Dispose();
            _reactiveDisposableBag = new DisposableBag();

            IsDie = Health.Select(i => i <= 0)
                          .DistinctUntilChanged()
                          .ToReadOnlyReactiveProperty()
                          .AddTo(ref _reactiveDisposableBag);
            
            Health.OnNext(Status.Hp);
        }

        // Character Pool에 등록 시 꼭 실행
        public void Release() {
            if (_characterBehaviour != null) {
                ClassPool.Release<CharacterBehaviour>(_characterBehaviour);
                _characterBehaviour = null;
            }

            if (_status != null) {
                ClassPool.Release<Status>(_status);
                _status = null;
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
            if (currentAnimation != null && animationName.Equals(currentAnimation.Animation.Name) && currentAnimation.Loop == loop) {
                return 0;
            }

            return SkeletonAnimation.StartAnimation(animationName, loop, hasExit)?.AnimationEnd ?? 0;
        }


        //일단을 1로 설정
        private ValueContext CreateValueContext(
            int levelOffset = 0,
            int gradeOffset = 0,
            int tierOffset  = 0
        ) {
            return new(
                new ValueProvider()
                    .Add("level", 1 + levelOffset)
                    .Add("grade", 1 + gradeOffset)
                    .Add("tier", 1 + tierOffset)
            );
        }
        //end class
    }
}