using Script.GamePlay.Service.Interface;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private readonly IGroupService _group;
        

        public StageManager(
            IGroupService  group
            ) {
            _group =  group;
        }
    }
}