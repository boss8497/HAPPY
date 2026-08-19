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
    
    public struct UnitCollisionResultEnable : IComponentData, IEnableableComponent { }

    public struct UnitSystemControlEnable : IComponentData, IEnableableComponent { }

    public struct UnitFallingEnable : IComponentData, IEnableableComponent { }

    public struct UnitData : IComponentData {
        public Entity                     Entity;
        public long                       Uid;
        public int                        Team;
        public UnityObjectRef<GameObject> GameObject;
        public byte                       IsPlayer;
        public float                      CollisionDelay;
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
    /// </summary>
    public struct JumpInputData : IComponentData {
        public byte Held;
    }

    /// <summary>
    /// ECS 낙하 런타임 데이터.
    /// Gravity / FallGravity 는 ConfigurationInfo에서 초기화.
    /// X 이동 차단은 RunningSystem이 MapGroundData.FallDeathEnabled + FallDeathY 를 직접 읽어 판단한다.
    /// </summary>
    public struct FallingData : IComponentData {
        public float FallVelocity;
        public float Gravity;
        public float FallGravity;
    }

    /// <summary>
    /// ECS 점프 런타임 데이터.
    /// Held 유지 중(Timer &lt; MaxJumpTime)에는 RiseSpeed로 등속 상승해 MaxJumpTime 시점에
    /// 정확히 Status.Jump 높이(RiseSpeed * MaxJumpTime)에 도달하고, 버튼을 떼거나
    /// MaxJumpTime을 넘으면 즉시 중력 낙하로 전환된다.
    /// </summary>
    public struct JumpingData : IComponentData {
        public float GroundY;
        public float MaxJumpTime;
        public float RiseSpeed;
        public float Gravity;
        public float FallGravity;
        public float Timer;
        public float JumpVelocity;
    }
}