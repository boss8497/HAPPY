using Script.GamePlay.Service.Interface;
using Script.Utility.Runtime;
using UnityEngine.TextCore.Text;
using VContainer;

namespace Script.GamePlay.Stage {
    public partial class StageManager {
        private readonly IGroupService   _group;
        private readonly IObjectResolver _resolver;
        

        public StageManager(
            IGroupService   group,
            IObjectResolver resolver
            ) {
            _group    = group;
            _resolver = resolver;
        }

        public IGroupService   Group          => _group;
        public IObjectResolver Resolver => _resolver;
    }
}