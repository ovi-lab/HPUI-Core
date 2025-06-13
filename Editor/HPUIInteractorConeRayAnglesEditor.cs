using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.Interaction;
using UnityEditor;
using UnityEngine;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIInteractorConeRayAngles), true)]
    public class HPUIInteractorConeRayAnglesEditor : UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[] {
            "IndexDistalAngles",
            "IndexIntermediateAngles",
            "IndexProximalAngles",
            "MiddleDistalAngles",
            "MiddleIntermediateAngles",
            "MiddleProximalAngles",
            "RingDistalAngles",
            "RingIntermediateAngles",
            "RingProximalAngles",
            "LittleDistalAngles",
            "LittleIntermediateAngles",
            "LittleProximalAngles"
        };
        private HPUIInteractorConeRayAngles t;
        private SerializedProperty indexDistalAnglesProp,
            indexIntermediateAnglesProp,
            indexProximalAnglesProp,
            middleDistalAnglesProp,
            middleIntermediateAnglesProp,
            middleProximalAnglesProp,
            ringDistalAnglesProp,
            ringIntermediateAnglesProp,
            ringProximalAnglesProp,
            littleDistalAnglesProp,
            littleIntermediateAnglesProp,
            littleProximalAnglesProp;

        protected void OnEnable()
        {
            t = target as HPUIInteractorConeRayAngles;

            indexDistalAnglesProp = serializedObject.FindProperty("IndexDistalAngles");
            indexIntermediateAnglesProp = serializedObject.FindProperty("IndexIntermediateAngles");
            indexProximalAnglesProp = serializedObject.FindProperty("IndexProximalAngles");
            middleDistalAnglesProp = serializedObject.FindProperty("MiddleDistalAngles");
            middleIntermediateAnglesProp = serializedObject.FindProperty("MiddleIntermediateAngles");
            middleProximalAnglesProp = serializedObject.FindProperty("MiddleProximalAngles");
            ringDistalAnglesProp = serializedObject.FindProperty("RingDistalAngles");
            ringIntermediateAnglesProp = serializedObject.FindProperty("RingIntermediateAngles");
            ringProximalAnglesProp = serializedObject.FindProperty("RingProximalAngles");
            littleDistalAnglesProp = serializedObject.FindProperty("LittleDistalAngles");
            littleIntermediateAnglesProp = serializedObject.FindProperty("LittleIntermediateAngles");
            littleProximalAnglesProp = serializedObject.FindProperty("LittleProximalAngles");
        }

        public override void OnInspectorGUI()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!excludedSerializedNames.Contains(iterator.name))
                {
                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
            }

            DrawAnglesProp(indexDistalAnglesProp, t.IndexDistalAngles);
            DrawAnglesProp(indexIntermediateAnglesProp, t.IndexIntermediateAngles);
            DrawAnglesProp(indexProximalAnglesProp, t.IndexProximalAngles);
            DrawAnglesProp(middleDistalAnglesProp, t.MiddleDistalAngles);
            DrawAnglesProp(middleIntermediateAnglesProp, t.MiddleIntermediateAngles);
            DrawAnglesProp(middleProximalAnglesProp, t.MiddleProximalAngles);
            DrawAnglesProp(ringDistalAnglesProp, t.RingDistalAngles);
            DrawAnglesProp(ringIntermediateAnglesProp, t.RingIntermediateAngles);
            DrawAnglesProp(ringProximalAnglesProp, t.RingProximalAngles);
            DrawAnglesProp(littleDistalAnglesProp, t.LittleDistalAngles);
            DrawAnglesProp(littleIntermediateAnglesProp, t.LittleIntermediateAngles);
            DrawAnglesProp(littleProximalAnglesProp, t.LittleProximalAngles);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAnglesProp(SerializedProperty prop, List<HPUIInteractorConeRayAngleSides> angles)
        {
            string details = "";
            bool noFallbackSide = true;
            foreach (HPUIInteractorConeRayAngleSides item in angles)
            {
                if (item.side == t.FallbackSide)
                {
                    noFallbackSide = false;
                }

                if (string.IsNullOrWhiteSpace(details))
                {
                    details += " (";
                }
                else
                {
                    details += ", ";
                }
                details += $"{item.side}:{item.rayAngles.Count()}";
            }

            if (string.IsNullOrWhiteSpace(details))
            {
                details = " (-)";
            }
            else
            {
                details += ")";
            }

            if (noFallbackSide)
            {
                EditorGUILayout.HelpBox($"Missing angles for {t.FallbackSide} in {prop.displayName}", MessageType.Warning);
            }

            if (angles.Select(el => el.side).Distinct().Count() != angles.Count)
            {
                EditorGUILayout.HelpBox($"Duplicate side entries for angles in {prop.displayName}", MessageType.Warning);
            }

            GUIContent content = new GUIContent(prop.displayName + details);
            EditorGUILayout.PropertyField(prop, content);
        }
    }


    [CustomPropertyDrawer(typeof(HPUIInteractorConeRayAngleSides), true)]
    public class HPUIInteractorConeRayAngleSidesDrawer : PropertyDrawer
    {
        private GUIContent sideContent = new GUIContent("Side");
        private GUIContent anglesContent = new GUIContent("Angels");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float sideHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("side"), true);
            float anglesHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rayAngles"), true);
            // return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            return sideHeight + anglesHeight + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty sideProp = property.FindPropertyRelative("side");
            SerializedProperty anglesProp = property.FindPropertyRelative("rayAngles");

            float width = EditorGUIUtility.currentViewWidth;
            float segmentHeight = EditorGUIUtility.singleLineHeight;

            // Calculate rects
            Rect sideRect = new Rect(position.x, position.y, position.width, segmentHeight);

            float anglesHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rayAngles"), true);
            Rect rayAnglesRect = new Rect(position.x + 0.1f,
                                          position.y + segmentHeight + EditorGUIUtility.standardVerticalSpacing,
                                          position.width,
                                          anglesHeight);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(sideRect, sideProp, sideContent);
            EditorGUI.PropertyField(rayAnglesRect, anglesProp, anglesContent);

            EditorGUI.EndProperty();
        }
    }
}
