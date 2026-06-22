using System;
using Script.GameInfo.Attribute;
using UnityEngine;

namespace Script.GameInfo.Dungeon {
    // 시각적 배경 타일 데이터.
    // 바닥 높이(groundY)는 HeightPoint 커브가 결정하므로 이 클래스에는 없다.
    [Serializable]
    public class MapTileData {
        public Vector3 position; // 타일 루트 월드 위치 (MapEditor에서 직접 배치)

        [AssetPath(typeof(GameObject))]
        public string prefabKey; // Addressable 프리팹 키

        // MapEditor Save 시 Tilemap.cellBounds 기준으로 계산 (런타임에서 직접 사용)
        public float width;   // 타일 너비 (월드 단위)
        public float centerX; // 시각적 중앙 X (cellBounds.center 기준)
        public float startX;  // 왼쪽 끝 X
        public float endX;    // 오른쪽 끝 X
    }
}
