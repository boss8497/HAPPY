namespace Script.GameInfo.Info.Enum {
    public enum StatType {
        Hp = 1,    // 체력
        Mp,        // 사실 안쓸거 같기는 한데 생성 해둠
        Atk,       // 공격 데미지
        Def,       // 방어력 ( 영향 안받는 stat -> Collision )
        Spd,       // 속도
        Jump,      // 점프력
        Collision, // CharacterInfo -> 캐릭터간 충돌 데미지,
                   // 상대방의 Collision Stat이 나에게 데미지를 줌 Atk과 다름. Atk는 발사체나 직접 공격에 사용할 예정
        Score,     // 점수
        Heart,     // HP 채워주는 아이템이 있을 수 있어서 일단 만들어 둠
        Max,
    }
}