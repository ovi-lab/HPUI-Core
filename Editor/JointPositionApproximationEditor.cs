using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Core.Tracking;
using ubco.ovilab.HPUI.Core.UI;
using System.Linq;

namespace ubco.ovilab.HPUI.Core.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JointPositionApproximation), true)]
    public class JointPositionApproximationEditor: UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[]{
            "ui", "windowSize", "maeThreshold"
        };
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Assets/HPUIContinousUI.prefab";
        private JointPositionApproximation t;
        private SerializedProperty uiProp, windowSizeProp, maeThresholdProp;
        private bool advancedFoldout = false;

        protected void OnEnable()
        {
            t = target as JointPositionApproximation;
            uiProp = serializedObject.FindProperty("ui");
            windowSizeProp = serializedObject.FindProperty("windowSize");
            maeThresholdProp = serializedObject.FindProperty("maeThreshold");
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

            EditorGUILayout.PropertyField(uiProp);

            advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced settings");
            if (advancedFoldout)
            {
                EditorGUILayout.PropertyField(windowSizeProp);
                EditorGUILayout.PropertyField(maeThresholdProp);
            }

            if (uiProp.objectReferenceValue == null)
            {
                if (GUILayout.Button(new GUIContent("Instantiate & add UI", "Instantiate UI for continuous interface generation and add it")))
                {
                    GameObject uiObj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefab)) as GameObject;
                    uiProp.objectReferenceValue = uiObj.GetComponent<HPUIGeneratedContinuousInteractableUI>();
                    Debug.Log($"[[[{uiProp.objectReferenceValue}]]]");
                }
            }

            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button(new GUIContent("Automated recompute", "Intiate the process to estimate the joint locations and generate the surface")))
            {
                foreach (Object t in targets)
                {
                    (t as JointPositionApproximation)?.AutomatedRecompute();
                }
            }
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
