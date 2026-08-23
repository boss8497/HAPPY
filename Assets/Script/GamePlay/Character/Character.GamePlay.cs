using System;
using System.Linq;
using Expression;
using Script.GameInfo.Character;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Pool;
using Script.GamePlay.Stat;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;

//Runtime에 생성되는 부분들
namespace Script.GamePlay.Character {
    public partial class Character {
        public override Vector2       Position      => Transform.position;
        public override Transform     Transform     => gameObject.transform;
        public override CharacterType CharacterType => CharacterInfo.type;
        public          GameObject    GameObject    => gameObject;
        public          bool          IsAlive       => this != null;


        private         bool _isPlayer;
        public override bool IsPlayer => _isPlayer;


        [SerializeReference, SerializeField, ReadOnly]
        private CharacterBehaviour _characterBehaviour;

        public CharacterBehaviour CharacterBehaviour => _characterBehaviour;


        // 기본적으로 Initialize할 때 찾지만 Prefab에서 등록 가능하도록 설정
        [ShowInInspector]
        private SkeletonAnimation _skeletonAnimation;

        public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;


        [SerializeReference, SerializeField]
        private DieAnimation _dieAnimation;

        public DieAnimation DieAnimation => _dieAnimation;

        private string[] _animationNames;

        public string[] SpineAnimationNames {
            get {
                _animationNames ??= SkeletonAnimation?.Skeleton.Data.Animations.Select(s => s.Name).ToArray() ?? Array.Empty<string>();
                return _animationNames;
            }
            set => _animationNames = value;
        }

        // Inspector에서 스텟을 디버깅하기 위해 ShowInInspector 설정
        [ShowInInspector, ReadOnly]
        private Status _status;

        public Status Status => _status;

        private void InitializeGamePlay() {
            //Spine 컴포넌트
            if (_skeletonAnimation == null) {
                _skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (_skeletonAnimation == null) {
                    _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
                }
            }

            //Die Animation
            if (_dieAnimation == null) {
                _dieAnimation = GetComponent<DieAnimation>();
                if (_dieAnimation == null) {
                    _dieAnimation = GetComponentInChildren<DieAnimation>();
                }
            }


            _characterBehaviour ??= ClassPool.Get<CharacterBehaviour>();
            _characterBehaviour.Initialize(BehaviourInfo, this);

            _status ??= ClassPool.Get<Status>();
        }


        private void ReleaseGamePlay() {
            ClassPool.Release<CharacterBehaviour>(_characterBehaviour);
            ClassPool.Release<Status>(_status);
            _status             = null;
            _characterBehaviour = null;
        }

        // 상대방에게 캐릭터를 다시 받는 DelayTime ECS에서 사용하고 이 시간이 지나야
        // 상대방과 충돌 시 데미지를 받음
        public override float GetCollisionDelayTime()
            => CharacterInfo?.type switch {
                _ => 1f
            };


        private ValueContext CreateValueContext(
            int levelOffset = 0,
            int gradeOffset = 0,
            int tierOffset  = 0
        ) {
            return new(
                new ValueProvider()
                    .Add("level", (Item?.CurrentValue?.Level?.CurrentValue ?? 1) + levelOffset)
                    .Add("grade", (Item?.CurrentValue?.Grade?.CurrentValue ?? 0) + gradeOffset)
                    .Add("tier", (Item?.CurrentValue?.Tier?.CurrentValue ?? 0) + tierOffset)
            );
        }
    }
}