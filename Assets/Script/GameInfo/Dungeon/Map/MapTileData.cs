using System;
using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    // 시각적 배경 타일 데이터.
    // 바닥 높이(groundY)는 HeightPoint 커브가 결정하므로 이 클래스에는 없다.
    [Serializable]
    public class MapTileData {
        public Vector3 position; // 타일 중앙 월드 위치 (MapEditor에서 직접 배치)

        [AssetPath(typeof(GameObject))]
        public string prefabKey; // Addressable 프리팹 키
    }
}
