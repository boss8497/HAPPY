using Script.GameInfo.Info.Character.Behaviour;
using Script.GamePlay.Character.Interface;
using UnityEngine;
using CharacterInfo = Script.GameInfo.Info.Character.CharacterInfo;

namespace Script.GamePlay.Character {
    public class Character : MonoBehaviour, ICharacter {

        public CharacterInfo info;

        public BehaviourInfo behaviourInfo;


    }
}