using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Script.GamePlay {
    [ExecuteInEditMode]
    public class SpineEditor : SerializedMonoBehaviour {
        private event Action EditorUpdateEvent;

        public SkeletonAnimation skeletonAnimation;
        public Vector3           targetPosition = new Vector3(-5f, 0, 0);

        [ValueDropdown("animations")]
        public string animation;

        [OnValueChanged("AnimationChanged")]
        private List<string> animations;

        public SkeletonData SkeletonData  => skeletonAnimation?.Skeleton?.Data ?? default;
        public string       AnimationName => skeletonAnimation?.AnimationName.ToString();
        public bool         Loop = false;

        private bool isPlaying = false;

        private double           lastEditorTime;
        private float            deltaTime;

        [HideInInspector] public float maxRange = 1; // 최대값을 동적으로 변경
        [HideInInspector] public float minRange = 0;

        [PropertyRange("@minRange", "@maxRange")]
        [OnValueChanged("SliderValueChanged")]
        public float currentAnimationTime = 0f;


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

        private void GetAnimaitionMaxTime() => maxRange = float.Parse(animation);

        private void AnimationChanged() {
            var currentAnimation = SkeletonData.FindAnimation(animation);
            maxRange = currentAnimation.Duration;
        }

        private void SliderValueChanged() {
            if (isPlaying) StopAnimation();
            var currentAnimation = SkeletonData.FindAnimation(animation);
            skeletonAnimation.state.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.LateUpdate(); // 즉시 갱신
            maxRange = currentAnimation.Duration;
            var track = skeletonAnimation?.AnimationState?.SetAnimation(0, currentAnimation.Name, false);
            //track.TrackTime = currentAnimationTime;
            skeletonAnimation?.AnimationState?.Update(currentAnimationTime);
            skeletonAnimation.LateUpdate();
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

            animations = skeletonAnimation.Skeleton?.Data.Animations.Select(s => s.Name).ToList() ?? new List<string>();
            animation  = animations?.FirstOrDefault();
        }

        [HorizontalGroup("Initialized", 0.5f)]
        [Button("애니메이션 초기화", ButtonSizes.Large)]
        public void AnimationReset() {
            isPlaying                   = false;
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


        [HideIf("isPlaying")]
        [Button("애니메이션 재생 ▶ ", ButtonSizes.Large), GUIColor(0, 1, 0)]
        public void StartAnimation() {
            isPlaying                   = true;
            skeletonAnimation.timeScale = 1.0f;

            if (Loop) {
                var currentAnimation = SkeletonData.FindAnimation(animation);
                maxRange = currentAnimation.Duration;
                if (currentAnimation != null) {
                    PlayAnimation(animation, Loop);
                }
            }
            else {
                var currentAnimation = SkeletonData.FindAnimation(animation);
                maxRange = currentAnimation.Duration;
                if (currentAnimation != null) {
                    PlayAnimation(animation, Loop);
                }
            }

            DeltaTimeUpdate();
            SceneView.RepaintAll();
        }

        [ShowIf("isPlaying")]
        [Button("애니메이션 중지 ■ ", ButtonSizes.Large), GUIColor(1, 0, 0)]
        public void StopAnimation() {
            isPlaying                   = false;
            skeletonAnimation.timeScale = 0f;

            SceneView.RepaintAll();
        }

        private void DeltaTimeUpdate() {
            var currentTime = EditorApplication.timeSinceStartup;
            deltaTime      = (float)(currentTime - lastEditorTime);
            lastEditorTime = currentTime;
        }

        private void PlayAnimation(string animationName, bool loop = false) {
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetToSetupPose();
            skeletonAnimation.LateUpdate();
            skeletonAnimation?.AnimationState?.SetAnimation(0, animationName, loop);
        }


        private void EditorUpdate() {
            if (isPlaying && Application.isPlaying == false) {
                currentAnimationTime = skeletonAnimation.AnimationState.GetCurrent(0).AnimationTime;
                DeltaTimeUpdate();
                skeletonAnimation.Update(deltaTime);
                skeletonAnimation.LateUpdate();
                EditorUpdateEvent?.Invoke();
                SceneView.RepaintAll();
                currentAnimationTime = skeletonAnimation.AnimationState.GetCurrent(0).AnimationTime;
                if (Loop == false && currentAnimationTime > maxRange) {
                    currentAnimationTime = 0;
                    StopAnimation();
                    skeletonAnimation.Update(0);
                    skeletonAnimation.LateUpdate();
                }
            }
        }

    }
}