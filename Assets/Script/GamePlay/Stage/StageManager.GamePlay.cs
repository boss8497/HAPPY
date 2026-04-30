using Script.GamePlay.Character;
using Script.GamePlay.ECS.Component;
using Script.Utility.Runtime;
using UnityEngine;
using UnityEngine.Pool;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private void ReleaseCharacter() {
            if (_players != null) {
                var characterPool = ListPool<ICharacter>.Get();
                characterPool.AddRange(_players);
                foreach (var character in characterPool) {
                    RemoveCharacter(character);
                }
                
                characterPool.Clear();
                ListPool.Return(characterPool);
                _players.Clear();
            }
            
            if (_enemies != null) {
                var characterPool = ListPool<ICharacter>.Get();
                characterPool.AddRange(_enemies);
                foreach (var character in _enemies) {
                    character.Release();
                    StagePooling?.Push(character.GameObject);
                }
                
                characterPool.Clear();
                ListPool.Return(characterPool);
                _enemies.Clear();
            }
        }
        
        public bool AddCharacter(GameObject obj) {
            // Child 검색 안하는 이유는 규칙상 Character Script가 최상위 GameObject여야 합니다.
            var characterScript = obj.GetComponent<ICharacter>();
            return AddCharacter(characterScript);
        }
        public bool AddCharacter(ICharacter characterScript) {
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다.");
                return false;
            }

            if (_vCamera.LookAt == null) {
                _vCamera.LookAt = _targetGroup.transform;
                _vCamera.Follow = _targetGroup.transform;;
            }
            //카메라 셋팅
            _targetGroup.AddMember(characterScript.Transform, 1, 1);

            characterScript.Initialize(0, true);
            _players.Add(characterScript);
            return true;
        }

        private bool RemoveCharacter(GameObject obj) {
            var characterScript = obj.GetComponent<ICharacter>();
            return RemoveCharacter(characterScript);
        }

        private bool RemoveCharacter(ICharacter characterScript) {
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다.");
                return false;
            }
            
            //카메라 셋팅
            _targetGroup.RemoveMember(characterScript.Transform);
            
            _players.Remove(characterScript);
            characterScript.Release();
            StagePooling.Push(characterScript.GameObject);
            return true;
        }

        public bool AddEnemy(GameObject obj) {
            var characterScript = obj.GetComponent<ICharacter>();
            return AddEnemy(characterScript);
        }
        
        public bool AddEnemy(ICharacter characterScript) {
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다.");
                return false;
            }
            characterScript.Initialize(1, false);
            _enemies.Add(characterScript);
            return true;
        }
        
        private bool RemoveEnemy(GameObject obj) {
            var characterScript = obj.GetComponent<ICharacter>();
            return RemoveEnemy(characterScript);
        }
        
        private bool RemoveEnemy(ICharacter characterScript) {
            if (characterScript == null) {
                Debug.LogError($"캐릭터 스크립트를 찾을 수 없습니다.");
                return false;
            }
            
            _enemies.Remove(characterScript);
            characterScript.Release();
            StagePooling.Push(characterScript.GameObject);
            return true;
        }

        public void AddItemScore(float score) {
            ItemScore.OnNext(ItemScore.Value + score);
        }
        
        public void Pause() {
            AddState(StageState.SystemControl);
            _gameTimer.Pause();
            SetPause(true);
        }

        public void Resume() {
            RemoveState(StageState.SystemControl);
            _gameTimer.Resume();
            SetPause(false);
        }
        
        private void SetPause(bool pause) {
            var entityManager = _entityWorld.EntityManager;
            var query         = entityManager.CreateEntityQuery(typeof(EGameTimer));
            if (query.IsEmptyIgnoreFilter) {
                Debug.LogWarning("GameTimeData singleton이 없습니다.");
                query.Dispose();
                return;
            }
            
            var entity   = query.GetSingletonEntity();
            var gameTime = entityManager.GetComponentData<EGameTimer>(entity);
            gameTime.IsPaused = pause;
            entityManager.SetComponentData(entity, gameTime);
            
            query.Dispose();
        }
    }
}