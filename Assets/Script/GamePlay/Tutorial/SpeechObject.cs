using System;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Script.Tutorial {
    public class SpeechObject : IDisposable {
        public Image         image;
        public RectTransform speechArrow;
        public RectTransform imageParent;

        private readonly AddressableHandle<Sprite> _iconAsset = new();

        public void SetImage(string imagePath) {
            image.sprite = _iconAsset.Load(imagePath);
        }

        public void On(string imagePath, bool flip) {
            image.sprite = _iconAsset.Load(imagePath);
            if (speechArrow != null) {
                speechArrow.gameObject.SetActive(true);
            }

            var scale = imageParent.localScale;
            if (flip) {
                imageParent.localScale = new(Mathf.Abs(scale.x) * -1f, scale.y, scale.z);
            }
            else {
                imageParent.localScale = new(Mathf.Abs(scale.x), scale.y, scale.z);
            }

            imageParent.gameObject.SetActive(true);
        }

        public void Off() {
            if (speechArrow != null) {
                speechArrow.gameObject.SetActive(false);
            }

            imageParent.gameObject.SetActive(false);
        }

        public void Dispose() {
            _iconAsset.Dispose();
        }
    }
}