using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Script.GUI.ScreenData;
using Script.GUI.ScreenData.Interface;
using TMPro;
using UnityEngine;

namespace Script.GUI.Screen {
    public class CountDown : Screen {
        [SerializeField]
        private TMP_Text countDownText;

        [SerializeField]
        private float moveDuration = 0.15f;

        [SerializeField]
        private float holdDuration = 0.35f;

        private RectTransform _textRect;
        private Sequence      _sequence;

        public override UniTask OpenInternal(IScreenOption screenOption, CancellationToken ct = default) {
            _textRect ??= (RectTransform)countDownText.transform;

            countDownText.SetText(screenOption is CountDownOption option
                ? option.Text
                : string.Empty);

            var parentWidth = ((RectTransform)_textRect.parent).rect.width;
            var offset      = parentWidth * 0.5f + _textRect.rect.width * 0.5f;

            _textRect.anchoredPosition = new Vector2(-offset, _textRect.anchoredPosition.y);
            

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                                .Append(_textRect.DOAnchorPosX(0f, moveDuration).SetEase(Ease.OutCubic))
                                .AppendInterval(holdDuration)
                                .Append(_textRect.DOAnchorPosX(offset, moveDuration).SetEase(Ease.InCubic));
            
            return UniTask.CompletedTask;
        }

        public override UniTask CloseInternal() {
            if (_sequence == null) 
                return UniTask.CompletedTask;

            _sequence.Kill();
            _sequence = null;
            return UniTask.CompletedTask;
        }
    }
}
