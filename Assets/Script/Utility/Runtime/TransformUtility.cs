using UnityEngine;

namespace Script.Utility.Runtime {
    public static class TransformUtility {
        public static void SetPositionX(this Transform transform, float x) {
            if (transform == null) return;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        public static void SetPositionY(this Transform transform, float y) {
            if (transform == null) return;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        public static void SetPositionZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.position = new Vector3(transform.position.x, transform.position.y, z);
        }

        public static void SetPositionXY(this Transform transform, float x, float y) {
            if (transform == null) return;
            transform.position = new Vector3(x, y, transform.position.z);
        }  
        public static void SetPositionXZ(this Transform transform, float x, float z) {
            if (transform == null) return;
            transform.position = new Vector3(x, transform.position.y, z);
        }
        public static void SetPositionYZ(this Transform transform, float y, float z) {
            if (transform == null) return;
            transform.position = new Vector3(transform.position.x, y, z);
        }
        

        public static void SetRotationX(this Transform transform, float x) {
            if (transform == null) return;
            transform.rotation = Quaternion.Euler(new Vector3(x, transform.rotation.y, transform.rotation.z));
        }

        public static void SetRotationY(this Transform transform, float y) {
            if (transform == null) return;
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, y, transform.rotation.z));
        }

        public static void SetRotationZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, transform.rotation.y, z));
        }

        public static void SetScaleX(this Transform transform, float x) {
            if (transform == null) return;
            transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
        }

        public static void SetScaleY(this Transform transform, float y) {
            if (transform == null) return;
            transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
        }

        public static void SetScaleZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
        }

        public static void SetLocalPositionX(this Transform transform, float x) {
            if (transform == null) return;
            transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
        }

        public static void SetLocalPositionY(this Transform transform, float y) {
            if (transform == null) return;
            transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
        }

        public static void SetLocalPositionZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
        }

        public static void SetLocalRotationX(this Transform transform, float x) {
            if (transform == null) return;
            transform.localRotation = Quaternion.Euler(new Vector3(x, transform.localRotation.y, transform.localRotation.z));
        }

        public static void SetLocalRotationY(this Transform transform, float y) {
            if (transform == null) return;
            transform.localRotation = Quaternion.Euler(new Vector3(transform.localRotation.x, y, transform.localRotation.z));
        }

        public static void SetLocalRotationZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.localRotation = Quaternion.Euler(new Vector3(transform.localRotation.x, transform.localRotation.y, z));
        }

        public static void SetLocalScaleX(this Transform transform, float x) {
            if (transform == null) return;
            transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
        }

        public static void SetLocalScaleY(this Transform transform, float y) {
            if (transform == null) return;
            transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
        }

        public static void SetLocalScaleZ(this Transform transform, float z) {
            if (transform == null) return;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
        }
    }
}