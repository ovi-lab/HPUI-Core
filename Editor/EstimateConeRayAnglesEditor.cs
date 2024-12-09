using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EstimateConeRayAngles), true)]
    public class EstimateConeRayAnglesEditor: UnityEditor.Editor
    {
        private static readonly string[] excludedSerializedNames = new string[]{"generatedConeRayAngles"};
        private EstimateConeRayAngles t;
        private bool startedEstimation = false;
        private SerializedObject generatedConeRayAnglesObj;

        private HPUIInteractorConeRayAngles angles;
        private string saveName = "Assets/NewHPUIInteractorConeRayAngles.asset";

        protected void OnEnable()
        {
            t = target as EstimateConeRayAngles;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = EditorApplication.isPlaying;
            if (!startedEstimation && GUILayout.Button(new GUIContent("Start estimation", "TODO")))
            {
                startedEstimation = true;
                t.StartEstimation();
            }
            if (startedEstimation && GUILayout.Button(new GUIContent("Finish estimation", "TODO")))
            {
                startedEstimation = false;
                angles = t.FinishEstimation();
                generatedConeRayAnglesObj = new SerializedObject(angles);
            }

            if (angles != null)
            {
                bool guiState = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("IndexDistalAngles"));
                EditorGUILayout.PropertyField(generatedConeRayAnglesObj.FindProperty("IndexIntermediateAngles"));
                GUI.enabled = guiState;

                saveName = EditorGUILayout.TextField("Save name", saveName);
                if(GUILayout.Button(new GUIContent("Save", "TODO")))
                {
                    AssetDatabase.CreateAsset(angles, saveName);
                    AssetDatabase.SaveAssets();
                }
            }

            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
