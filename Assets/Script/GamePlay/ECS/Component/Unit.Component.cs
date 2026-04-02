using Script.GameInfo.Character;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Script.GamePlay.ECS.Component {
    public struct UnitEntityTag : IComponentData { }

    public struct UnitData : IComponentData {
        public long                       Uid;
        public int                        Team;
        public int                        InstanceId;
        public UnityObjectRef<GameObject> GameObject;
    }

    public enum HitboxType : byte {
        None   = 0,
        Rect   = 1,
        Circle = 2,
    }

    public struct HitboxState : IComponentData {
        public CharacterState Current;
        public CharacterState Applied;
    }

    public struct HitboxPresetRange : IBufferElementData {
        public CharacterState StateMask;
        public int            StartIndex;
        public int            Length;
    }

    public struct HitboxPresetShape : IBufferElementData {
        public HitboxType Type;
        public float3     Offset;
        public float3     Size;
        public float      Radius;
    }

    public struct HitboxActiveShape : IBufferElementData {
        public HitboxType Type;
        public float3     Offset;
        public float3     Size;
        public float      Radius;
    }

    public struct CollisionResultData : IBufferElementData {
        public Entity OtherEntity;
        public long   OtherUid;
        public int    OtherTeam;
    }


    /// <summary>
    /// Running 전용 이동 데이터
    /// Enabled == true 일 때만 시스템이 이동 처리
    /// </summary>
    public struct RunningData : IComponentData, IEnableableComponent {
        public float3 Direction;
        public float  Speed;
    }
}