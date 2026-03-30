using Script.GameData.Data.Interface;
using Script.GameData.Model;

namespace Script.GamePlay.Service.Interface {
    public interface IGroupService : IService{
        IGroupData GroupData { get; }
    }
}