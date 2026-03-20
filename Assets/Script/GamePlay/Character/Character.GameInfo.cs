using Script.GameInfo.Attribute;
using Script.GameInfo.Base;
using Script.GameInfo.Character;
using Script.GameInfo.Table;

//GameInfo(기획 데이터)를 가져오는 cs
namespace Script.GamePlay.Character {
    public partial class Character {
        [Character]
        public int characterInfoUid;


        private CharacterInfo _characterInfo;
        public CharacterInfo CharacterCharacterInfo {
            get {
                if (InfoBase.ValidUid(characterInfoUid) == false) {
                    return null;
                }

                if (_characterInfo == null || _characterInfo.UID != characterInfoUid) {
                    _characterInfo = GameInfoManager.Instance.Get<CharacterInfo>(characterInfoUid);
                }

                return _characterInfo;
            }
        }

        private BehaviourInfo _behaviourInfo;
        public BehaviourInfo BehaviourInfo {
            get {
                if (CharacterCharacterInfo == null) return null;
                if (InfoBase.ValidUid(CharacterCharacterInfo.behaviourId) == false) {
                    return null;
                }

                if (_behaviourInfo == null || _behaviourInfo.UID != CharacterCharacterInfo.behaviourId) {
                    _behaviourInfo = GameInfoManager.Instance.Get<BehaviourInfo>(CharacterCharacterInfo.behaviourId);
                }

                return _behaviourInfo;
            }
        }
    }
}