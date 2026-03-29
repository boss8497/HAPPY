using R3;
using Script.GameData.Model;

namespace Script.GameData.Data.Interface {
    public interface IGroupData {
        ReactiveProperty<GroupModel> Model { get; }
        
    }
}