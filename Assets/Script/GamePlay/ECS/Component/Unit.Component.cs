using System;
using Unity.Entities;
using Unity.Mathematics;
using Script.GameInfo.Character;
using UnityEngine;

namespace Script.GamePlay.ECS.Component {
    public struct UnitEntityTag : IComponentData { }

    public struct UnitDieEnable : IComponentData, IEnableableComponent { }

    public struct UnitRunningEnable : IComponentData, IEnableableComponent { }

    public struct UnitJumpingEnable : IComponentData, IEnableableComponent { }

    public struct UnitCollisionEnable : IComponentData, IEnableableComponent { }

    public struct UnitSystemControlEnable : IComponentData, IEnableableComponent { }

    public struct UnitData : IComponentData {
        public Entity                     Entity;
        public long                       Uid;
        public int                        Team;
        public int                        InstanceId;
        public UnityObjectRef<GameObject> GameObject;
    }

    public struct HitBoxData : IComponentData {
        public HitBoxType Type;
        public float3 Offset;
        // Rect
        public float3 Size;
        // Radius
        public float Radius;

        public HitBoxData(Hitbox info) {
            if (info == null) {
                Type   = HitBoxType.Invisible;
                Offset = float3.zero;
                Size   = float3.zero;
                Radius = 0f;
            }
            else {
                Type   = info.type;
                Offset = info.offset;
                Size   = info.size;
                Radius = info.radius;
            }
        }
    }

    public struct UnitCollisionResult : IBufferElementData {
        public Entity OtherEntity;
        public long   OtherUid;
        public int    OtherTeam;
    }

    public struct UnitCollisionDelay : IBufferElementData {
        public long  OtherUid;
        public float ExpireTime;
    }


    /// <summary>
    /// Running 전용 이동 데이터
    /// Enabled == true 일 때만 시스템이 이동 처리
    /// </summary>
    public struct RunningData : IComponentData {
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
    public struct JumpingData : IComponentData {
        public float GroundY;
        public float CurrentJumpTime;
        public float MaxJumpTime;
        public float MinJumpTime;
        public float Gravity;
        public float FallGravity;
        public float Timer;
        public float JumpVelocity;
    }
}