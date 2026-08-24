using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GameInfo.Table;
using Script.GamePlay.Service.Interface;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Script.Tutorial {
    public class FocusTest : MonoBehaviour {
        private ITutorialService _tutorialService;

        [Tutorial]
        public int testTutorialInfo;
        
        [Button]
        public async void Test() {
            _tutorialService.StartTutorial(testTutorialInfo);
        }
        
        [Inject]
        public void InjectSelf(
            ITutorialService  tutorialService
        ) {
            _tutorialService = tutorialService;
        }
    }
}