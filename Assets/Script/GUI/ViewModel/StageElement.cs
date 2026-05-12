using System;
using Cysharp.Threading.Tasks;
using R3;
using Script.GameInfo.Dungeon;
using Script.GamePlay.Scene;
using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.GUI.ViewModel {
    public class StageElement : MonoBehaviour {
        // Reactive
        private IGroupService   _groupService;
        private ISceneLoader    _sceneLoader;
        
        [Inject]
        public void Inject(
            IGroupService   groupService,
            ISceneLoader    sceneLoader
        ) {
            _groupService   = groupService;
            _sceneLoader    = sceneLoader;
        }
        
        
        [SerializeField]
        private TMP_Text indexText;

        [SerializeField]
        private Button startBtn;


        public ReactiveProperty<Stage>       Stage       { get; set; } = new();
        public ReactiveProperty<DungeonInfo> DungeonInfo { get; set; } = new();


        private DisposableBag _disposableBag;

        private void Awake() {
            if(startBtn != null) {
                startBtn.ClickAddListener(() => {
                    if (Stage?.CurrentValue == null) return;
                    _sceneLoader.LoadScene(Stage.CurrentValue.scenePath).Forget();
                });
            }
        }


        public void InitializeReactive() {
            _disposableBag = new();

            Stage.CombineLatest(DungeonInfo, (stage, dungeonInfo) => (stage, dungeonInfo))
                 .Subscribe(data => {
                     if (data.dungeonInfo == null || data.stage == null) return;
                     indexText.SetText($"{data.dungeonInfo.stages.FindIndex(s => s.guid.Value == data.stage.guid.Value) + 1}");
                 })
                 .AddTo(ref _disposableBag);
        }

        public void SetReactive(Stage value, DungeonInfo sub) {
            DungeonInfo.OnNext(sub);
            Stage.OnNext(value);
        }

        private void OnDisable() {
            Release();
        }

        public void Release() {
            _disposableBag.Dispose();
        }
    }
}