using System;

namespace Script.GameInfo.Dungeon {
    // 바닥 높이를 결정하는 포인트.
    // HeightPoint 들을 연결한 커브가 캐릭터의 groundY를 결정한다.
    //   Point(x=0,  groundY=0)  → Point(x=50, groundY=0)  : x 0~50 구간 바닥 Y=0 (평지)
    //   Point(x=50, groundY=0)  → Point(x=100,groundY=10) : x 50~100 구간 Y가 0→10 으로 보간
    [Serializable]
    public class HeightPoint {
        public float               x;             // 월드 X 위치
        public float               groundY;       // 이 지점의 바닥 Y 높이
        public HeightInterpolation interpolation; // 이 Point → 다음 Point 사이의 보간 방식
    }
}
