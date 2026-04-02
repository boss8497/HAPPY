using System;
using System.Linq;
using Expression;
using Script.GameInfo.Info.Stat;
using Script.GameInfo.Table;
using Script.GamePlay.Stat;
using Script.Utility.Runtime;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;

//Runtime에 생성되는 부분들
namespace Script.GamePlay.Character {
    public partial class Character {
        public override Vector2   Position  => Transform.position;
        public override Transform Transform => gameObject.transform;

        private         bool _isPlayer;
        public override bool IsPlayer => _isPlayer;


        [SerializeReference, ReadOnly]
        private CharacterBehaviour _characterBehaviour;

        public CharacterBehaviour CharacterBehaviour => _characterBehaviour;


        // 기본적으로 Initialize할 때 찾지만 Prefab에서 등록 가능하도록 설정
        [ShowInInspector]
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

        // Inspector에서 스텟을 디버깅하기 위해 ShowInInspector 설정
        [ShowInInspector, ReadOnly]
        private Status _status;

        public Status Status => _status;


        private void InitializeGamePlay() {
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


            using var _ = CreateValueContext();
            foreach (var statusUid in _characterInfo.statusUids) {
                _status.Add(GameInfoManager.Instance.Get<StatusInfo>(statusUid));
            }
        }


        private void ReleaseGamePlay() {
            if (_characterBehaviour != null) {
                ClassPool.Release<CharacterBehaviour>(_characterBehaviour);
                _characterBehaviour = null;
            }

            if (_status != null) {
                ClassPool.Release<Status>(_status);
                _status = null;
            }
        }


        //TODO: Database 관련 로직이 없기 때문에 일단은 여기서 테스트 설정
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
    }
}