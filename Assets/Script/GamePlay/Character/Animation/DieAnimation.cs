using Cysharp.Threading.Tasks;
using UnityEngine;


// 죽는 애니메이션은 캐릭터마다 다를 수 있기 때문에 추상 클래스로 만들어서 상속받아서 구현하게 하자
namespace Script.GamePlay.Character {
    public abstract class DieAnimation : MonoBehaviour {
        public abstract UniTask PlayAnimation(); 
        public abstract UniTask ResetAnimation(); 
    }
}