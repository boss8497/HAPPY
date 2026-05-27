using Spine;
using Spine.Unity;

namespace Script.Utility.Runtime {
    public static class SpineUtility {
        private enum AnimationName {
            IDLE,
            RUN,
            ATTACK,
            STAND,
            DIE,
            DAMAGE,
        }
        
        public static TrackEntry StartAnimation(this SkeletonAnimation skeletonAnimation, string animationName, bool loop = false, bool hasExit = false) {
            if (skeletonAnimation == null) return null;
            if (hasExit) skeletonAnimation.AnimationState.ClearTracks();
            return skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
        }

        public static float GetAnimationTime(this SkeletonAnimation skeletonAnimation, string aniName) {
            if (skeletonAnimation == null) return 0;
            var entry = skeletonAnimation.AnimationState.Data.SkeletonData.FindAnimation(aniName);
            return entry?.Duration ?? 0;
        }
        
        public static TrackEntry StartAnimation(this SkeletonGraphic skeletonGraphic, string aniName, bool loop = false) {
            var state = skeletonGraphic.AnimationState;

            // 🔹 이미 같은 애니메이션이 재생 중이면 return
            var current = state.GetCurrent(0);
            if (current is { Animation: not null } &&
                current.Loop == loop &&
                current.Animation.Name == aniName.ToString())
            {
                return null;
            }

            return state.SetAnimation(0, aniName, loop);
        }
    }
}