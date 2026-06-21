using Script.GamePlay.ECS.Component;
using Unity.Entities;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private Entity _mapGroundEntity;

        public float GroundY { get; private set; }

        private void InitializeMapGround() {
            var em = _entityWorld.EntityManager;

            if (_mapGroundEntity == Entity.Null || !em.Exists(_mapGroundEntity)) {
                _mapGroundEntity = em.CreateEntity();
                em.AddComponentData(_mapGroundEntity, new MapGroundData { GroundY = 0f });
            }
            else {
                em.SetComponentData(_mapGroundEntity, new MapGroundData { GroundY = 0f });
            }

            GroundY = 0f;
        }

        public void SetGroundY(float groundY) {
            if (Mathf.Approximately(GroundY, groundY)) return;

            GroundY = groundY;

            var em = _entityWorld.EntityManager;
            if (_mapGroundEntity == Entity.Null || !em.Exists(_mapGroundEntity)) {
                _mapGroundEntity = em.CreateEntity();
                em.AddComponentData(_mapGroundEntity, new MapGroundData { GroundY = groundY });
                return;
            }

            em.SetComponentData(_mapGroundEntity, new MapGroundData { GroundY = groundY });
        }

        private void ReleaseMapGround() {
            if (_mapGroundEntity != Entity.Null
                && _entityWorld.IsAlive
                && _entityWorld.EntityManager.Exists(_mapGroundEntity)) {
                _entityWorld.EntityManager.DestroyEntity(_mapGroundEntity);
            }

            _mapGroundEntity = Entity.Null;
            GroundY          = 0f;
        }
    }
}
