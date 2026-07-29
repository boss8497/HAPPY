using System.Linq;
using SW.GUI.Base;

namespace SW.GUI {
    public class SW_GUI_BUTTON_GROUP : SW_GUI_BUTTON_GROUP_BASE {
        protected override void InitializeAwake() {
            base.InitializeAwake();

            for (int i = 0; i < _elementsList.Count; i++) {
                if (i == 0) {
                    Select(_elementsList[i]);
                }
                else {
                    DeSelect(_elementsList[i]);
                }
            }
            
        }
    }
}