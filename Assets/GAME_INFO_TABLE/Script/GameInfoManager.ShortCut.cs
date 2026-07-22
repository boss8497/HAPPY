using System.Linq;
using Script.GameInfo.Dungeon;

namespace Script.GameInfo.Table {
    public partial class GameInfoManager {
        private DungeonInfo _lobbyDungeonInfo;
        public  DungeonInfo LobbyDungeonInfo => _lobbyDungeonInfo ??= GetCollection<DungeonInfo>().FirstOrDefault(r => r.category == Category.Lobby);
    }
}