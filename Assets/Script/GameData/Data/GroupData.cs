using System;
using R3;
using Script.GameData.Data.Interface;
using Script.GameData.Model;

namespace Script.GameData.Data {
    public class GroupData : IGroupData, IDisposable {
        public ReactiveProperty<GroupModel> Model { get; private set; } = new();

        private DisposableBag _disposableBag;

        public GroupData(GroupModel model) {
            
            Update(model);
        }

        public void Update(GroupModel model) {
            Model.OnNext(model);
        }

        public void Dispose() {
            _disposableBag.Dispose();
            Model?.Dispose();
        }
    }
}