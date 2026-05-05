using Script.DataBase.Interface;
using Script.LifetimeScope.Locator;
using VContainer.Unity;

namespace Script.Client {
    /// <summary>
    /// 각 종 플러그인 (Firebase, Steam, AD) 등 필요한 플러그인을 초기화
    /// Client -> Server 연결 및 통신
    /// Server와 통신은 GameClient.Client.cs 파일에 구현
    /// </summary>
    public partial class GameClient : IInitializable {
        private readonly IScopeLocator _scopeLocator;
        private readonly IDataBase     _dataBase;


        public GameClient(
            IScopeLocator scopeLocator,
            IDataBase     dataBase
        ) {
            _scopeLocator = scopeLocator;
            _dataBase     = dataBase;
        }


        public void Initialize() { }
    }
}