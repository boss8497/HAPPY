using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spine;
using Spine.Unity;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Script.GamePlay {
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class SpineEditor : MonoBehaviour {
#if UNITY_EDITOR
        //따로 애니메이션 이벤트가 생기면 생성
        private event Action EditorUpdateEvent;

        public SkeletonAnimation skeletonAnimation;
        public Vector3           targetPosition = new Vector3(-5f, 0, 0);

        [ValueDropdown("_animations")]
        public string animationName;

        [OnValueChanged("AnimationChanged")]
        private List<string> _animations;

        public SkeletonData SkeletonData  => skeletonAnimation?.Skeleton?.Data;
        public bool         loopOption;

        private bool _isPlaying;

        private double           _lastEditorTime;
        private float            _deltaTime;

        [HideInInspector] public float maxRange = 1; // 최대값을 동적으로 변경
        [HideInInspector] public float minRange;

        [PropertyRange("@minRange", "@maxRange")]
        [OnValueChanged("SliderValueChanged")]
        public float currentAnimationTime;


        private void OnEnable() {
            if (!Application.isPlaying) {
                EditorApplication.update += EditorUpdate;
            }
        }

        private void OnDisable() {
            if (!Application.isPlaying) {
                EditorApplication.update -= EditorUpdate;
            }
        }

        private void GetAnimaitionMaxTime() => maxRange = float.Parse(animationName);

        private void AnimationChanged() {
            var currentAnimation = SkeletonData.FindAnimation(animationName);
            maxRange = currentAnimation.Duration;
        }

        private void SliderValueChanged() {
            if (_isPlaying) StopAnimation();
            var currentAnimation = SkeletonData.FindAnimation(animationName);
            skeletonAnimation.state.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.LateUpdate(); // 즉시 갱신
            maxRange = currentAnimation.Duration;
            skeletonAnimation?.AnimationState?.SetAnimation(0, currentAnimation.Name, false);
            //track.TrackTime = currentAnimationTime;
            skeletonAnimation?.AnimationState?.Update(currentAnimationTime);
            skeletonAnimation?.LateUpdate();
            SceneView.RepaintAll();
        }


        [HorizontalGroup("Initialized", 0.5f)]
        [Button("데이터 초기화", ButtonSizes.Large)]
        public void Initialize() {
            if (skeletonAnimation == null) {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
                if (skeletonAnimation == null) {
                    skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
                }
            }

            _animations = skeletonAnimation.Skeleton?.Data.Animations.Select(s => s.Name).ToList() ?? new List<string>();
            animationName  = _animations?.FirstOrDefault();
        }

        [HorizontalGroup("Initialized", 0.5f)]
        [Button("애니메이션 초기화", ButtonSizes.Large)]
        public void AnimationReset() {
            _isPlaying                   = false;
            currentAnimationTime        = 0;
            skeletonAnimation.timeScale = 1.0f;
            skeletonAnimation.state.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.LateUpdate(); // 즉시 갱신

            PlayAnimation("IDLE");
            skeletonAnimation.Update(0);
            skeletonAnimation.LateUpdate();

            SceneView.RepaintAll();
        }


        [HideIf("_isPlaying")]
        [Button("애니메이션 재생 ▶ ", ButtonSizes.Large), GUIColor(0, 1, 0)]
        public void StartAnimation() {
            _isPlaying                   = true;
            skeletonAnimation.timeScale = 1.0f;

            if (loopOption) {
                var currentAnimation = SkeletonData.FindAnimation(animationName);
                if(currentAnimation != null) {
                    maxRange = currentAnimation.Duration;
                    PlayAnimation(animationName, loopOption);
                }
            }
            else {
                var currentAnimation = SkeletonData.FindAnimation(animationName);
                if(currentAnimation != null) {
                    maxRange = currentAnimation.Duration;
                    PlayAnimation(animationName, loopOption);
                }
            }

            DeltaTimeUpdate();
            SceneView.RepaintAll();
        }

        [ShowIf("_isPlaying")]
        [Button("애니메이션 중지 ■ ", ButtonSizes.Large), GUIColor(1, 0, 0)]
        public void StopAnimation() {
            _isPlaying                   = false;
            skeletonAnimation.timeScale = 0f;

            SceneView.RepaintAll();
        }

        private void DeltaTimeUpdate() {
            var currentTime = EditorApplication.timeSinceStartup;
            _deltaTime      = (float)(currentTime - _lastEditorTime);
            _lastEditorTime = currentTime;
        }

        private void PlayAnimation(string aniName, bool loop = false) {
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.LateUpdate();
            skeletonAnimation?.AnimationState?.SetAnimation(0, aniName, loop);
        }


        private void EditorUpdate() {
            if (_isPlaying && Application.isPlaying == false) {
                currentAnimationTime = skeletonAnimation.AnimationState.GetCurrent(0).AnimationTime;
                DeltaTimeUpdate();
                skeletonAnimation.Update(_deltaTime);
                skeletonAnimation.LateUpdate();
                EditorUpdateEvent?.Invoke();
                SceneView.RepaintAll();
                currentAnimationTime = skeletonAnimation.AnimationState.GetCurrent(0).AnimationTime;
                if (loopOption == false && currentAnimationTime > maxRange) {
                    currentAnimationTime = 0;
                    StopAnimation();
                    skeletonAnimation.Update(0);
                    skeletonAnimation.LateUpdate();
                }
            }
        }
    }
    
#endif
}