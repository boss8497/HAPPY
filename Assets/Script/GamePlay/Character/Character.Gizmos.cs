#if UNITY_EDITOR
using Script.GameInfo.Character;
using Script.GameInfo.Table;
using Sirenix.OdinInspector;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public partial class Character {
        private bool _useGizmos;
        
        //시스템에서 전체로 보일 수 있을때를 대비해서
        private bool UseGizmos => _useGizmos;



        [HideIf("UseGizmos")]
        [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void ShowGizmos() {
            hitBox          = CharacterInfo.hitbox;
            this._useGizmos = !this._useGizmos;
        }

        [ShowIf("UseGizmos")]
        [Button(ButtonSizes.Large), GUIColor(1, 0.2f, 0)]
        private void HideGizmos() {
            hitBox          = null;
            this._useGizmos = !this._useGizmos;
        }

        
        [SerializeReference, ShowIf("UseGizmos")]
        public Hitbox hitBox;

        [ShowIf("UseGizmos")]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 0f, 1)]
        private void ApplyHitBox() {
            CharacterInfo.hitbox = hitBox;
            GameInfoManager.Instance.Save<CharacterInfo>(CharacterInfo);
        }


        private void OnDrawGizmos() {
            if (UseGizmos == false) return;

            if (hitBox == null) return;
            
            // transform 기준으로 local offset을 그대로 사용 가능하게 함
            Gizmos.matrix = transform.localToWorldMatrix;

            if (hitBox == null)
                return;

            switch (hitBox.type) {
                case HitBoxType.Rect:
                    DrawRectHitbox(hitBox);
                    break;

                case HitBoxType.Circle:
                    DrawCircleHitbox(hitBox);
                    break;
            }

            // 다른 Gizmo에 영향 없게 원복
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawRectHitbox(Hitbox hitbox) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(hitbox.offset, hitbox.size);
        }

        private void DrawCircleHitbox(Hitbox hitbox) {
            Gizmos.color = Color.red;

            // Gizmos는 "원"이 아니라 sphere만 기본 제공
            // XY 2D 원처럼 보이게 하려면 아래 커스텀 함수 사용
            DrawWireCircleXY(hitbox.offset, hitbox.radius, 32);
        }

        private void DrawWireCircleXY(Vector3 center, float radius, int segments) {
            if (segments < 3)
                segments = 3;

            var angleStep = 360f / segments;
            var prevPoint = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * radius;

            for (int i = 1; i <= segments; i++) {
                var angle     = angleStep * i * Mathf.Deg2Rad;
                var nextPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}

#endif