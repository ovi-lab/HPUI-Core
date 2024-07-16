using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Tracking;
using ubco.ovilab.HPUI.UI;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JointPositionApproximation), true)]
    public class JointPositionApproximationEditor: UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[]{"ui"};
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Assets/HPUIContinousUI.prefab";
        private JointPositionApproximation t;
        private SerializedProperty uiProp;

        protected void OnEnable()
        {
            t = target as JointPositionApproximation;
            uiProp = serializedObject.FindProperty("ui");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (uiProp.objectReferenceValue == null)
            {
                if (GUILayout.Button(new GUIContent("Instantiate & add UI", "Instantiate UI for continuous interface generation and add it")))
                {
                    GameObject uiObj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefab)) as GameObject;
                    uiProp.objectReferenceValue = uiObj.GetComponent<HPUIContinuousInteractableUI>();
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
