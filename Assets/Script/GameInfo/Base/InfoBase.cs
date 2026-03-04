using System;
using Script.GameInfo.Attribute;

namespace Script.GameInfo.Base {
    public class InfoBase {
        public int    UID { get; set; }
        public string ID  { get; set; }

        [LocalizePath("@\"Item/Name/\" + guid", false)]
        public string Name { get; set; }

        public IComponent[] Components { get; set; } = Array.Empty<IComponent>();
    }
}