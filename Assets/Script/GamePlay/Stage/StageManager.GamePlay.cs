using UnityEngine;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        public bool AddCharacter(GameObject obj) {
            var characterScript = obj.GetComponent<Character.ICharacter>() ?? obj.GetComponentInChildren<Character.ICharacter>();
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다. GameObject: {obj.name}");
                return false;
            }

            //카메라 셋팅
            _targetGroup.AddMember(obj.transform, 1, 1);

            characterScript.Initialize(0, true);
            _players.Add(characterScript);
            return true;
        }

        public bool AddEnemy(GameObject obj) {
            var characterScript = obj.GetComponent<Character.ICharacter>() ?? obj.GetComponentInChildren<Character.ICharacter>();
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다. GameObject: {obj.name}");
                return false;
            }

            characterScript.Initialize(1, false);
            _enemies.Add(characterScript);
            return true;
        }

        public void AddItemScore(float score) {
            ItemScore.OnNext(ItemScore.Value + score);
        }
    }
}