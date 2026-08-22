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
            var tutorialInfo = GameInfoManager.Instance.Get<TutorialInfo>(testTutorialInfo);
            foreach (var set in tutorialInfo.sets) {
                if (set is FocusGuide focusGuide) {
                    await _tutorialService.StartFocusAsync(focusGuide, () => {
                        _tutorialService.StopFocusAsync(true);
                    });
                }
            }
        }
        
        [Inject]
        public void InjectSelf(
            ITutorialService  tutorialService
        ) {
            _tutorialService = tutorialService;
        }
    }
}