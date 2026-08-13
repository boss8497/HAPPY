using Cysharp.Threading.Tasks;
using Script.GamePlay.Service.Interface;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.Tutorial {
    public class FocusComponent : MonoBehaviour {
        private ITutorialService _tutorialService;
        
        [SerializeField, SerializeReference]
        private TutorialFocusData focusData;

        public TutorialFocusData FocusData => focusData;

        public bool showGizmos = false;
        
        
        [Inject]
        public void InjectSelf(
            ITutorialService  tutorialService
        ) {
            _tutorialService = tutorialService;
        }

        private void Start() {
            Initialized().Forget();
        }

        private async UniTask Initialized(bool register = true) {
            await UniTask.WaitUntil(() => _tutorialService != null);
            
            if (focusData.rtf == null) {
                focusData.rtf = transform as RectTransform;
            }

            switch (focusData.type) {
                case FocusType.Button:
                    if (focusData.target == null) {
                        focusData.target = transform.GetComponent<Image>();
                    }

                    break;
                case FocusType.Image:
                    if (focusData.target == null) {
                        focusData.target = transform.GetComponent<Image>();
                    }

                    break;
                case FocusType.Toggle:
                    if (focusData.target == null) {
                        focusData.target = transform.GetComponent<Image>();
                    }

                    break;
            }

            if (register) {
                _tutorialService.RegisterFocusData(focusData);
            }
        }


        private void OnDestroy() {
            if (_tutorialService != null && focusData != null) {
                _tutorialService.UnRegisterFocusData(focusData);
            }
        }

        public void CreateFocusData(string id, FocusType type) {
            focusData = new() {
                id   = id,
                type = type,
                rtf  = transform as RectTransform
            };
        }

        public void ReleaseFocusData() {
            focusData = null;
        }


        void OnDrawGizmos() {
            if (showGizmos == false) return;

            var target         = focusData.rtf;
            var sizeOffset     = focusData.sizeOffset;
            var positionOffset = focusData.positionOffset;
            if (!target) return;

            // 1) RectTransform의 로컬 rect (pivot 반영된 로컬 좌표 사각형)
            Rect r = target.rect;

            // 2) sizeOffset 적용 (양쪽으로 늘리려면 중심 기준으로 확장)
            float w = r.width + sizeOffset.x;
            float h = r.height + sizeOffset.y;

            // pivot 기준 로컬 rect를 다시 구성
            // target.rect는 이미 pivot이 반영된 로컬 min/max를 가지고 있지만,
            // sizeOffset으로 확장하려면 pivot을 기준으로 새 rect를 만드는 게 안전함.
            Vector2 pivot = target.pivot;

            Vector2 min = new Vector2(-pivot.x * w, -pivot.y * h);
            Vector2 max = min + new Vector2(w, h);

            // 3) positionOffset 적용 (로컬 X/Y)
            min += positionOffset;
            max += positionOffset;

            // 4) 로컬 코너 4개 -> 월드로 변환
            Vector3[] corners = {
                new Vector3(min.x, min.y, 0f), // BL
                new Vector3(min.x, max.y, 0f), // TL
                new Vector3(max.x, max.y, 0f), // TR
                new Vector3(max.x, min.y, 0f), // BR
            };

            for (int i = 0; i < corners.Length; i++)
                corners[i] = target.TransformPoint(corners[i]);

            // 5) Gizmos로 사각형 라인 그리기
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
    }
}