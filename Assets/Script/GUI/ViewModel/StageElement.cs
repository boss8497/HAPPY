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
        private IGroupService _groupService;

        [Inject]
        public void Inject(
            IGroupService groupService
        ) {
            _groupService = groupService;
        }


        [SerializeField]
        private TMP_Text indexText;

        [SerializeField]
        public Button startBtn;
        public Button errorBtn;


        public ReactiveProperty<Stage>        Stage         { get; set; } = new();
        public ReactiveProperty<DungeonInfo>  DungeonInfo   { get; set; } = new();
        public ReadOnlyReactiveProperty<bool> CanEnterStage { get; set; }


        private DisposableBag _disposableBag;


        public void InitializeReactive() {
            _disposableBag = new();

            CanEnterStage = DungeonInfo.CombineLatest(Stage, (dungeon, stage) => (dungeon, stage))
                                       .Select(i => {
                                           if (i.dungeon == null || i.stage == null) return false;
                                           return _groupService.CanEnterStage(i.dungeon, i.stage);
                                       })
                                       .DistinctUntilChanged()
                                       .ToReadOnlyReactiveProperty()
                                       .AddTo(ref _disposableBag);

            Stage.CombineLatest(DungeonInfo, (stage, dungeonInfo) => (stage, dungeonInfo))
                 .Subscribe(data => {
                     if (data.dungeonInfo == null || data.stage == null) return;
                     indexText.SetText($"{data.dungeonInfo.stages.FindIndex(s => s.guid.Value == data.stage.guid.Value) + 1}");
                 })
                 .AddTo(ref _disposableBag);

            CanEnterStage.Subscribe(canEnter => {
                             errorBtn.SetActiveSafe(canEnter == false);
                             if (startBtn != null) {
                                 startBtn.interactable = canEnter;
                             }
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