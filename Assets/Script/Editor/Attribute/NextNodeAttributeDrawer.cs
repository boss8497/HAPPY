using System;
using System.Linq;
using Script.GameInfo.Attribute;
using Script.GameInfo.Info;
using Script.GameInfo.Info.Character;
using Script.Utility.Runtime;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Attribute {
    public class NextNodeAttributeDrawer : OdinAttributeDrawer<NextNodeAttribute> {
        private SerializeGuid Guid{
            get {
                return Property.ValueEntry.TypeOfValue switch {
                    { } t when t == typeof(Guid)         => (Guid)Property.ValueEntry.WeakSmartValue,
                    {} t when t == typeof(SerializeGuid) => (SerializeGuid)Property.ValueEntry.WeakSmartValue,
                    { } t when t == typeof(string)       => Property.ValueEntry.WeakSmartValue is string guidString ? string.IsNullOrEmpty(guidString) || guidString.ToLower().Equals("none") 
                                                                                                                          ? System.Guid.Empty : System.Guid.Parse(guidString) : System.Guid.Empty,
                    _                                    => System.Guid.Empty
                };
            }
            set {
                switch (Property.ValueEntry.TypeOfValue) {
                    case { } t when t == typeof(Guid):
                        Property.ValueEntry.WeakSmartValue = value;
                        break;
                    case { } t when t == typeof(SerializeGuid):
                        Property.ValueEntry.WeakSmartValue = new SerializeGuid(value);
                        break;
                    case { } t when t == typeof(string):
                        Property.ValueEntry.WeakSmartValue = value.ToString();
                        break;
                }
                Property.ValueEntry.ApplyChanges();
            }
        }
        
        private NodeBase[] _stateNodes;
        private string[]   _nodeName;
        
        
        protected override void Initialize() {
            base.Initialize();

            var parent = Property.Parent;
        
            while (parent != null && parent.ValueEntry.WeakSmartValue is not BehaviourInfo) {
                parent = parent.Parent;
            }
        
            if(parent == null || parent.ValueEntry.WeakSmartValue is not BehaviourInfo) {
                _stateNodes = Array.Empty<NodeBase>();
                _nodeName  = Array.Empty<string>();
                return;
            }
        
            var ipNodes = parent.Children.FirstOrDefault(i => i.Name == nameof(BehaviourInfo.nodes));
            if (ipNodes == null) {
                _stateNodes = Array.Empty<NodeBase>();
                _nodeName  = Array.Empty<string>();
                return;
            }

            _stateNodes = ipNodes.ValueEntry.WeakSmartValue as NodeBase[] ?? Array.Empty<NodeBase>();
            _nodeName  = _stateNodes.Select(r => r?.id ?? "None")
                                   .ToArray();
            
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            EditorGUILayout.BeginHorizontal();
            var index = _stateNodes.FindIndex(r => Guid.Equals(r?.guid));
            index = EditorGUILayout.Popup(
                label
              , index
              , _nodeName
            );

            Guid = index >= 0 && _stateNodes[index] != null ? _stateNodes[index].guid : SerializeGuid.Empty();
                
            EditorGUILayout.EndHorizontal();
        }
    }
}