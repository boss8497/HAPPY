namespace Script.GameInfo.Character {
    public enum HitBoxType {
        Circle,
        Rect,
        Invisible, // 무적 상태라고 보면 될듯. 사용 의도는 State가 변할 때 한해서 HitBox를 생성 기본은 무적
    }
}