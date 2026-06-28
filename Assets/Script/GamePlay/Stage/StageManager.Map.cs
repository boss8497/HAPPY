using Script.GamePlay.ECS.Component;
using Unity.Entities;
using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private Entity _mapGroundEntity;

        public float GroundY          { get; private set; }
        public float FallDeathY       { get; private set; }
        public bool  FallDeathEnabled { get; private set; }

        private void InitializeMapGround() {
            var em = _entityWorld.EntityManager;

            if (_mapGroundEntity == Entity.Null || !em.Exists(_mapGroundEntity)) {
                _mapGroundEntity = em.CreateEntity();
                em.AddComponentData(_mapGroundEntity, new MapGroundData { GroundY = 0f, FallDeathY = 0f, FallDeathEnabled = 0 });
            }
            else {
                em.SetComponentData(_mapGroundEntity, new MapGroundData { GroundY = 0f, FallDeathY = 0f, FallDeathEnabled = 0 });
            }

            GroundY          = 0f;
            FallDeathY       = 0f;
            FallDeathEnabled = false;
        }

        public void SetMapGroundData(float groundY, float fallDeathY, bool hasFallDeathY) {
            GroundY          = groundY;
            FallDeathY       = fallDeathY;
            FallDeathEnabled = hasFallDeathY;

            var em = _entityWorld.EntityManager;
            if (_mapGroundEntity == Entity.Null || !em.Exists(_mapGroundEntity)) {
                _mapGroundEntity = em.CreateEntity();
                em.AddComponentData(_mapGroundEntity, new MapGroundData {
                    GroundY          = groundY,
                    FallDeathY       = fallDeathY,
                    FallDeathEnabled = hasFallDeathY ? (byte)1 : (byte)0,
                });
                return;
            }

            em.SetComponentData(_mapGroundEntity, new MapGroundData {
                GroundY          = groundY,
                FallDeathY       = fallDeathY,
                FallDeathEnabled = hasFallDeathY ? (byte)1 : (byte)0,
            });
        }

        private void ReleaseMapGround() {
            if (_mapGroundEntity != Entity.Null
                && _entityWorld.IsAlive
                && _entityWorld.EntityManager.Exists(_mapGroundEntity)) {
                _entityWorld.EntityManager.DestroyEntity(_mapGroundEntity);
            }

            _mapGroundEntity = Entity.Null;
            GroundY          = 0f;
            FallDeathY       = 0f;
            FallDeathEnabled = false;
        }
    }
}
