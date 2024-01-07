using ubco.ovilab.HPUI.Utils;
using UnityEditor;
using UnityEngine;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
    public class ConditionalFieldAttributeDrawer : PropertyDrawer
    {
        private SerializedProperty conditionalProp;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalFieldAttribute labelAttribute = attribute as ConditionalFieldAttribute;

            if (conditionalProp == null)
            {
                conditionalProp = property.serializedObject.FindProperty(labelAttribute.conditionalProp);
                if (conditionalProp.propertyType != SerializedPropertyType.Boolean)
                {
                    conditionalProp = null;
                    Debug.LogError($"The property {labelAttribute.conditionalProp} is not of type bool.");
                }
            }

            GUI.enabled = conditionalProp.boolValue;
            EditorGUI.PropertyField(position, property);
            GUI.enabled = true;
        }
    }
}
