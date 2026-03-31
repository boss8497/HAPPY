using Script.GameInfo.Character;
using Unity.Entities;
using Unity.Mathematics;

namespace Script.GamePlay.ECS.Component {
    
    public struct UnitEntityTag : IComponentData  {
    }
    
    public struct UnitIdentityData : IComponentData {
        public long Uid;
        public int  Team;
        public int  InstanceId;
    }
    
    public struct UnitTransformData : IComponentData {
        public float2 Position;
    }
    
    
    public enum CharacterHitboxShapeType : byte {
        None   = 0,
        Rect   = 1,
        Circle = 2,
    }

    public struct CharacterHitboxStateData : IComponentData {
        public CharacterState Current;
        public CharacterState Applied;
    }

    public struct CharacterHitboxPresetRangeData : IBufferElementData {
        public CharacterState StateMask;
        public int            StartIndex;
        public int            Length;
    }

    public struct CharacterHitboxPresetShapeData : IBufferElementData {
        public CharacterHitboxShapeType ShapeType;
        public float2                   Offset;
        public float2                   Size;
        public float                    Radius;
    }

    public struct CharacterHitboxActiveShapeData : IBufferElementData {
        public CharacterHitboxShapeType ShapeType;
        public float2                   Offset;
        public float2                   Size;
        public float                    Radius;
    }
    
    public struct CharacterCollisionResultData : IBufferElementData {
        public Entity OtherEntity;
        public long   OtherUid;
        public int    OtherTeam;
    }
}