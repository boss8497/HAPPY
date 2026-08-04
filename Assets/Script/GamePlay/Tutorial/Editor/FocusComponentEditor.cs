using Codice.CM.Common;
using Script.GameInfo;
using SW.GUI.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Script.Tutorial.Editor {
    [CustomEditor(typeof(FocusComponent))]
    public class FocusComponentEditor : UnityEditor.Editor {
        private VisualElement        root;
        private VisualElement        guiElement;
        private VisualElement        scriptDataElement;
        private VisualElement        itemGuidListElement;
        private VisualElement        fieldsListElement;
        private PopupField<ItemInfo> focusDataSelector;
        private PopupField<string>   fieldsDataSelector;

        private SerializedProperty spFocusData;
        private SerializedProperty spShowGizmos;


        private FocusComponent    focusObject => serializedObject.targetObject as FocusComponent;
        private TutorialFocusData focusData   => spFocusData.managedReferenceValue as TutorialFocusData;

        public override VisualElement CreateInspectorGUI() {
            spFocusData       = serializedObject.FindProperty("focusData");
            spShowGizmos      = serializedObject.FindProperty("showGizmos");

            root                = new();
            guiElement          = new();
            scriptDataElement   = new();
            itemGuidListElement = new();
            fieldsListElement   = new();
            focusDataSelector   = new();
            fieldsDataSelector  = new();

            Refresh();

            return root;
        }

        private void Refresh() {
            guiElement.Clear();
            ScriptDataGUI();

            root.Add(guiElement);
        }

        private void ScriptDataGUI() {
            scriptDataElement.Clear();
            itemGuidListElement.Clear();
            fieldsListElement.Clear();

            spFocusData       = serializedObject.FindProperty("focusData");


            if (spFocusData.managedReferenceValue == null) {
                focusObject.CreateFocusData(string.Empty, FocusType.None);
                spFocusData.serializedObject.ApplyModifiedProperties();
                WaitForFrameRefreshUI(3);
                return;
            }


            var spIdField             = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.id));
            var spTypeField           = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.type));
            var spTargetField         = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.target));
            var spUseGardField        = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.useGard));
            var spPositionOffsetField = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.positionOffset));
            var spSizeOffsetField     = spFocusData.FindPropertyRelative(nameof(TutorialFocusData.sizeOffset));

            var createScriptElement = new VisualElement();

            var newGuidBtn = new Button() {
                text = "New Guid",
            };

            newGuidBtn.clicked += () => {
                focusData.guid = SerializeGuid.NewGuid();
                Refresh();
            };
            createScriptElement.Add(newGuidBtn);

            var guidField = new TextField {
                label       = "GUID",
                value       = focusData.guid.ToString(),
                enabledSelf = false
            };
            createScriptElement.Add(guidField);


            var idField = new TextField { label = "ID" };
            idField.BindProperty(spIdField);
            createScriptElement.Add(idField);

            var typeField = new EnumField(FocusType.None) { label = "Type" };
            typeField.BindProperty(spTypeField);
            createScriptElement.Add(typeField);
            typeField.RegisterValueChangedCallback(evt => {
                if (evt.newValue?.ToString() == evt.previousValue.ToString()) return;

                spTypeField.serializedObject.ApplyModifiedProperties();
                WaitForFrameRefreshUI(3);
            });

            var useGardField = new Toggle() { label = "Use Gard" };
            useGardField.BindProperty(spUseGardField);
            createScriptElement.Add(useGardField);


            var positionOffsetField = new Vector2Field() { label = "Position Offset" };
            positionOffsetField.BindProperty(spPositionOffsetField);
            createScriptElement.Add(positionOffsetField);

            var sizeOffsetField = new Vector2Field() { label = "Size Offset" };
            sizeOffsetField.BindProperty(spSizeOffsetField);
            createScriptElement.Add(sizeOffsetField);


            switch (focusData.type) {
                case FocusType.Button: {
                    var targetField = new PropertyField() { label = "TFButton" };
                    targetField.BindProperty(spTargetField);
                    createScriptElement.Add(targetField);
                    targetField.RegisterValueChangeCallback(evt => {
                        if (focusData.target == null) return;

                        if (focusData.target is not SW_GUI_BUTTON_BASE) {
                            var targetScript = focusData.target?.gameObject?.GetComponent<SW_GUI_BUTTON_BASE>();
                            focusData.target = targetScript;
                        }
                    });
                }
                    break;
                case FocusType.Image: {
                    var targetField = new PropertyField() { label = "TFImage" };
                    targetField.BindProperty(spTargetField);
                    createScriptElement.Add(targetField);
                    targetField.RegisterValueChangeCallback(evt => {
                        if (focusData.target == null) return;

                        if (focusData.target is not Image) {
                            var targetScript = focusData.target?.gameObject?.GetComponent<Image>();
                            focusData.target = targetScript;
                        }
                    });
                }
                    break;

                case FocusType.Toggle: {
                    var targetField = new PropertyField() { label = "TFToggle" };
                    targetField.BindProperty(spTargetField);
                    createScriptElement.Add(targetField);
                    targetField.RegisterValueChangeCallback(evt => {
                        if (focusData.target == null) return;

                        if (focusData.target is not SW_GUI_TOGGLE_BASE) {
                            var targetScript = focusData.target?.gameObject?.GetComponent<SW_GUI_TOGGLE_BASE>();
                            focusData.target = targetScript;
                        }
                    });
                }
                    break;
            }

            var showGizmosField = new Toggle() { label = "에디터 가이드 라인" };
            showGizmosField.BindProperty(spShowGizmos);
            createScriptElement.Add(showGizmosField);

            scriptDataElement.Add(createScriptElement);
            guiElement.Add(scriptDataElement);
        }


        private void WaitForFrameRefreshUI(int maxFrame) {
            var frame = 0;

            void WaitForUpdate() {
                EditorApplication.update -= WaitForUpdate;
                ++frame;
                if (frame < maxFrame) {
                    EditorApplication.update += WaitForUpdate;
                    return;
                }

                Refresh();
            }

            EditorApplication.update += WaitForUpdate;
        }
    }
}