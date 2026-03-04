using Script.GameInfo.Attribute;
using Script.Utility;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEngine;

namespace Script.Editor.Attribute {
    public class LocalizePathPropertyDrawer : OdinAttributeDrawer<LocalizePathAttribute, string> {
        protected override void DrawPropertyLayout(GUIContent label) {
            var valueResolver = ValueResolver.GetForString(Property, Attribute.Path);
            if (valueResolver.HasError) {
                valueResolver.DrawError();
                return;
            }

            var resolvedText  = valueResolver.GetValue();
            var propertyValue = (string)Property.ValueEntry.WeakSmartValue;
            if (string.IsNullOrEmpty(propertyValue) == false && string.IsNullOrEmpty(resolvedText) == false) {
                LocalizeUtility.SetLocalizeText(resolvedText, propertyValue);
            }

            var localizeText = Attribute.IsTextArea ? LocalizeUtility.LocalizeTextArea(resolvedText) : LocalizeUtility.LocalizeTextField(label, resolvedText);

            Property.ValueEntry.WeakSmartValue = localizeText;
        }
    }
}