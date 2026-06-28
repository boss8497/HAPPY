using System;
using Script.GameInfo.Enum;

namespace Script.GameInfo.Dungeon {
    [Serializable]
    public class MapSpawnAction : ActionBase {
        public MapSpawnAction() {
            // 매 Update마다 카메라 범위를 확인해야 하므로 Update 타이밍으로 기본 설정
            timing = EventTiming.Update;
        }

        // 시각적 배경 타일 목록 (위치 + 프리팹만 담는다)
        public MapTileData[] tiles = Array.Empty<MapTileData>();

        // 바닥 Y 높이를 정의하는 포인트 목록 (X 기준 오름차순으로 배치)
        // Point → Point 사이를 각 Point의 interpolation 방식으로 보간하여 캐릭터 groundY를 결정.
        // 구간별 낙사 X차단 라인은 HeightPoint.hasFallDeathY / fallDeathY 로 설정한다.
        public HeightPoint[] heightPoints = Array.Empty<HeightPoint>();
    }
}
