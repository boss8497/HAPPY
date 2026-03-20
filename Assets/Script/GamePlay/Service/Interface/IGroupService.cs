using Script.GameData.Model;

namespace Script.GamePlay.Service.Interface {
    public interface IGroupService : IService{
        GroupData GroupData { get; }
    }
}