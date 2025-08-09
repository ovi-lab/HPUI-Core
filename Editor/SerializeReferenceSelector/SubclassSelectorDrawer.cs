using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using ubco.ovilab.HPUI.utils;

namespace ubco.ovilab.HPUI.Editor
{
    /// This is taken from https://github.com/mackysoft/Unity-SerializeReferenceExtensions
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        static readonly int maxTypePopupLineCount = 13;
        static readonly Type unityObjectType = typeof(UnityEngine.Object);
        static readonly Dictionary<Type, PropertyDrawer> drawerCaches = new Dictionary<Type, PropertyDrawer>();
        static readonly GUIContent contentUknownDisplayName = new GUIContent("Unknown");
        static readonly GUIContent contentIsNotManagedReferenceLabel = new GUIContent("The property type is not manage reference.");

        readonly Dictionary<string,AdvancedTypePopup> typePopups = new Dictionary<string,AdvancedTypePopup>();
        readonly Dictionary<string,GUIContent> typeNameCaches = new Dictionary<string,GUIContent>();

        private SerializedProperty targetProperty;

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Render label first to avoid label overlap for lists
                Rect foldoutLabelRect = new Rect(position);
                foldoutLabelRect.height = EditorGUIUtility.singleLineHeight;
                foldoutLabelRect = EditorGUI.IndentedRect(foldoutLabelRect);
                Rect popupPosition = EditorGUI.PrefixLabel(foldoutLabelRect, label);

                // Draw the subclass selector popup.
                if (EditorGUI.DropdownButton(popupPosition, GetTypeName(property), FocusType.Keyboard))
                {
                    AdvancedTypePopup popup = GetTypePopup(property);
                    targetProperty = property;
                    popup.Show(popupPosition);
                }

                // Draw the foldout.
                if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
                {
                    Rect foldoutRect = new Rect(position);
                    foldoutRect.height = EditorGUIUtility.singleLineHeight;

                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
                }

                // Draw property if expanded.
                if (property.isExpanded)
                {
                    Rect rectBox = EditorGUILayout.BeginVertical(GUI.skin.box);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        Rect childPosition = position;
                        childPosition.y += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

                        GUIStyle style = new GUIStyle(GUI.skin.label);
                        style.fontStyle = FontStyle.BoldAndItalic;
                        GUIContent referenceName = new GUIContent(property.managedReferenceFullTypename);
                        float height = style.CalcSize(referenceName).y;
                        childPosition.height = height;
                        EditorGUI.LabelField(childPosition, referenceName, style);

                        // Check if a custom property drawer exists for this type.
                        PropertyDrawer customDrawer = GetCustomPropertyDrawer(property);
                        if (customDrawer != null)
                        {
                            // Draw the property with custom property drawer.
                            Rect indentedRect = position;
                            float foldoutDifference = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            indentedRect.height = customDrawer.GetPropertyHeight(property, label);
                            indentedRect.y += foldoutDifference;
                            customDrawer.OnGUI(indentedRect, property, label);
                        }
                        else if (!property.hasVisibleChildren)
                        {
                            childPosition.y += (height + EditorGUIUtility.standardVerticalSpacing);
                            EditorGUI.LabelField(childPosition, "No configurations");
                        }
                        else
                        {
                            childPosition.y += (height + EditorGUIUtility.standardVerticalSpacing);
                            foreach (SerializedProperty childProperty in GetChildProperties(property))
                            {
                                height = EditorGUI.GetPropertyHeight(childProperty, new GUIContent(childProperty.displayName, childProperty.tooltip), true);
                                childPosition.height = height;
                                EditorGUI.PropertyField(childPosition, childProperty, true);

                                childPosition.y += height + EditorGUIUtility.standardVerticalSpacing;
                            }
                        }
                    }
                    EditorGUILayout.Separator();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }
            }
            else
            {
                EditorGUI.LabelField(position, label, contentIsNotManagedReferenceLabel);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Tries to get the custom property drawer if one exists.
        /// </summary>
        private PropertyDrawer GetCustomPropertyDrawer (SerializedProperty property)
        {
            Type propertyType = GetType(property.managedReferenceFullTypename);
            PropertyDrawer drawer = null;

            if (propertyType != null && !drawerCaches.TryGetValue(propertyType, out drawer))
            {
                Type drawerType = GetCustomPropertyDrawerType(propertyType);
                drawer = (drawerType != null) ? (PropertyDrawer)Activator.CreateInstance(drawerType) : null;

                drawerCaches.Add(propertyType, drawer);
            }

            if (propertyType != null && drawer != null)
            {
                return drawer;
            }
            return null;
        }

        /// <summary>
        /// Gets the searchable popup to set values.
        /// </summary>
        private AdvancedTypePopup GetTypePopup (SerializedProperty property)
        {
            // Cache this string. This property internally call Assembly.GetName, which result in a large allocation.
            string managedReferenceFieldTypename = property.managedReferenceFieldTypename;

            if (!typePopups.TryGetValue(managedReferenceFieldTypename,out AdvancedTypePopup popup))
            {
                AdvancedDropdownState state = new AdvancedDropdownState();

                Type baseType = GetType(managedReferenceFieldTypename);
                IEnumerable<Type> types = TypeCache
                    .GetTypesDerivedFrom(baseType)
                    .Append(baseType)
                    .Where(p =>
                           (p.IsPublic || p.IsNestedPublic || p.IsNestedPrivate) &&
                           !p.IsAbstract &&
                           !p.IsGenericType &&
                           !unityObjectType.IsAssignableFrom(p) &&
                           Attribute.IsDefined(p, typeof(SerializableAttribute)));

                popup = new AdvancedTypePopup(types, Math.Min(maxTypePopupLineCount, types.Count()), state);

                popup.OnItemSelected += item =>
                {
                    Type type = item.Type;

                    // Apply changes to individual serialized objects.
                    foreach (var targetObject in targetProperty.serializedObject.targetObjects) {
                        SerializedObject individualObject = new SerializedObject(targetObject);
                        SerializedProperty individualProperty = individualObject.FindProperty(targetProperty.propertyPath);
                        object obj = SetManagedReference(individualProperty, type);
                        individualProperty.isExpanded = (obj != null);

                        individualObject.ApplyModifiedProperties();
                        individualObject.Update();
                    }
                };

                typePopups.Add(managedReferenceFieldTypename, popup);
            }
            return popup;
        }

        /// <summary>
        /// Gets the GUIContent of the SerializedProperty that is decorated.
        /// </summary>
        private GUIContent GetTypeName (SerializedProperty property)
        {
            // Cache this string.
            string managedReferenceFullTypename = property.managedReferenceFullTypename;

            if (string.IsNullOrEmpty(managedReferenceFullTypename))
            {
                return contentUknownDisplayName;
            }

            if (typeNameCaches.TryGetValue(managedReferenceFullTypename,out GUIContent cachedTypeName))
            {
                return cachedTypeName;
            }

            Type type = GetType(managedReferenceFullTypename);
            string typeName = null;

            typeName = ObjectNames.NicifyVariableName(type.Name);

            GUIContent result = new GUIContent(typeName);
            typeNameCaches.Add(managedReferenceFullTypename,result);
            return result;
        }

        /// <inheritdoc />
        public override float GetPropertyHeight (SerializedProperty property,GUIContent label) {
            PropertyDrawer customDrawer = GetCustomPropertyDrawer(property);
            if (customDrawer != null)
            {
                return property.isExpanded ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +  customDrawer.GetPropertyHeight(property,label):EditorGUIUtility.singleLineHeight;
            }
            else
            {
                if (property.isExpanded)
                {
                    return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }
        }

        /// <summary>
        /// Get <see cref="Type"/> from typeName
        /// </summary>
        internal static Type GetType (string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            int splitIndex = typeName.IndexOf(' ');
            var assembly = Assembly.Load(typeName.Substring(0, splitIndex));
            return assembly.GetType(typeName.Substring(splitIndex + 1));
        }

        /// <summary>
        /// Sets and returns the managed reference.
        /// </summary>
        internal static object SetManagedReference (SerializedProperty property,Type type)
        {
            object result = null;

            if ((type != null) && (property.managedReferenceValue != null))
            {
                // Restore an previous values from json.
                string json = JsonUtility.ToJson(property.managedReferenceValue);
                result = JsonUtility.FromJson(json, type);
            }

            if (result == null)
            {
                result = (type != null) ? Activator.CreateInstance(type) : null;
            }
			
            property.managedReferenceValue = result;
            return result;
        }

        /// <summary>
        /// Enumerator to iterate on the child properties of a SerializedProperty.
        /// </summary>
        internal static IEnumerable<SerializedProperty> GetChildProperties(SerializedProperty parent, int depth = 1)
        {
            parent = parent.Copy();

            int depthOfParent = parent.depth;
            var enumerator = parent.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not SerializedProperty childProperty)
                {
                    continue;
                }
                if (childProperty.depth > (depthOfParent + depth))
                {
                    continue;
                }
                yield return childProperty.Copy();
            }
        }

        /// <summary>
        /// Helper to <see cref="GetCustomPropertyDrawer"/>
        /// </summary>
        private static Type GetCustomPropertyDrawerType (Type type)
        {
            Type[] interfaceTypes = type.GetInterfaces();

            var types = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
            foreach (Type drawerType in types)
            {
                var customPropertyDrawerAttributes = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
                foreach (CustomPropertyDrawer customPropertyDrawer in customPropertyDrawerAttributes)
                {
                    var field = customPropertyDrawer.GetType().GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var fieldType = field.GetValue(customPropertyDrawer) as Type;
                        if (fieldType != null)
                        {
                            if (fieldType == type)
                            {
                                return drawerType;
                            }
							
                            // If the property drawer also allows for being applied to child classes, check if they match
                            var useForChildrenField = customPropertyDrawer.GetType().GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (useForChildrenField != null)
                            {
                                object useForChildrenValue = useForChildrenField.GetValue(customPropertyDrawer);
                                if (useForChildrenValue is bool && (bool)useForChildrenValue)
                                {
                                    // Check interfaces
                                    if (Array.Exists(interfaceTypes, interfaceType => interfaceType == fieldType))
                                    {
                                        return drawerType;
                                    }

                                    // Check derived types
                                    Type baseType = type.BaseType;
                                    while (baseType != null)
                                    {
                                        if (baseType == fieldType)
                                        {
                                            return drawerType;
                                        }

                                        baseType = baseType.BaseType;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
