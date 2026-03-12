using Spine;
using Spine.Unity;

namespace Script.Utility.Runtime {
    public static class SpineUtility {
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
    }
}