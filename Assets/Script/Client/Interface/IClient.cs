using Cysharp.Threading.Tasks;
using Script.GameData.Model;

namespace Script.Client {
    public interface IClient {
        UniTask<GroupModel> Req_Group();
        UniTask             Req_SaveGroup(GroupModel model);
    }
}