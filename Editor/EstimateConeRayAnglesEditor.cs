using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EstimateConeRayAngles), true)]
    public class EstimateConeRayAnglesEditor: UnityEditor.Editor
    {
        private enum State { Wait, Started, Processing }

        private static readonly string[] excludedSerializedNames = new string[]{"generatedConeRayAngles"};
        private EstimateConeRayAngles t;
        private State state = State.Wait;
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
            if (state == State.Wait && GUILayout.Button(new GUIContent("Start estimation", "TODO")))
            {
                state = State.Started;
                t.StartEstimation();
            }

            if (state == State.Started && GUILayout.Button(new GUIContent("Finish estimation", "TODO")))
            {
                state = State.Processing;
                t.FinishEstimation((angles) =>
                {
                    state = State.Wait;
                    this.angles = angles;
                });
            }

            if (state == State.Processing)
            {
                EditorGUILayout.LabelField("Processing...");
            }

            if (angles != null)
            {
                generatedConeRayAnglesObj = new SerializedObject(angles);
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
