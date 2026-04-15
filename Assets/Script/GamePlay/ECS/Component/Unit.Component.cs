using System;
using Script.GameInfo.Character;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Script.GamePlay.ECS.Component {
    public struct UnitEntityTag : IComponentData { }

    public struct UnitDieTag : IComponentData, IEnableableComponent { }

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

    public struct UnitCollisionResult : IBufferElementData {
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


    /// <summary>
    /// Character -> ECS 입력 전달용
    /// Held: 지금 누르고 있는지
    /// ReleaseRequested: 버튼을 뗀 순간 1회 전달용
    /// </summary>
    public struct JumpInputData : IComponentData {
        public byte Held;
        public byte ReleaseRequested;
    }

    /// <summary>
    /// ECS 점프 런타임 데이터
    /// 기존 JumpingAsync의 지역변수들을 옮긴 것
    /// </summary>
    public struct JumpingData : IComponentData, IEnableableComponent {
        public float GroundY;
        public float CurrentJumpTime;
        public float MaxJumpTime;
        public float MinJumpTime;
        public float Gravity;
        public float FallGravity;
        public float Timer;
        public float JumpVelocity;
    }

    /// <summary>
    /// ECS -> Character 결과 전달용
    /// 착지했는지 알려주는 간단한 브리지
    /// </summary>
    public struct JumpResultData : IComponentData {
        public byte Landed;
    }
}