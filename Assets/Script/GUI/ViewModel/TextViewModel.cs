using Script.Localize.Text;
using TMPro;
using UnityEngine;

namespace Script.GUI.ViewModel {
    public class TextViewModel : ViewModel {
        [SerializeField]
        private TMP_Text     text;
        
        [SerializeField]
        private LocalizeText localizeText;


        public override void InitializeInternal() {
            text.SetText(localizeText.GetText());
        }

        public override void DisableInternal() {
        }

        public override void DisposeInternal() {
        }
    }
}