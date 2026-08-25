using System;
using R3;
using Script.GameData.Data.Interface;
using Script.GameData.Model;
using Script.GameInfo.Info;

namespace Script.GameData.Data {
    public class GroupData : IGroupData, IDisposable {
        public ReactiveProperty<GroupModel> Model { get; private set; } = new();

        public ReadOnlyReactiveProperty<TutorialProgress> TutorialProgress { get; private set; }

        private DisposableBag _disposableBag;

        public GroupData(GroupModel model) {
            TutorialProgress = Model.Select(i => i?.tutorialProgress ?? GameInfo.Info.TutorialProgress.None).ToReadOnlyReactiveProperty().AddTo(ref _disposableBag);

            Update(model);
        }

        public void Update(GroupModel model) {
            Model.OnNext(model);
        }

        public bool CanPlayTutorial(TutorialProgress progress) {
            if (Model?.Value == null) return false;
            return (int)progress == (int)TutorialProgress.CurrentValue + 1;
        }

        public void Dispose() {
            _disposableBag.Dispose();
            Model?.Dispose();
        }
    }
}