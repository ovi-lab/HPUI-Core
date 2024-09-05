using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIInteractorFullRangeAngles), true)]
    public class HPUIInteractorFullRangeAnglesEditor: UnityEditor.Editor
    {
        private HPUIInteractorFullRangeAngles t;
        private bool generating = false;
        private float maxAngle = 90;
        private float angleStep = 5;
        private float raySelectionThreshold = 0.015f;

        protected void OnEnable()
        {
            t = target as HPUIInteractorFullRangeAngles;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!generating && GUILayout.Button("Generate new values"))
            {
                generating = true;
            }

            if (generating)
            {
                string message = "This will generate the angles.";
                if (t.angles.Count != 0)
                {
                    EditorGUILayout.HelpBox("There are already values configured. Generating would overwrite it!", MessageType.Warning);
                    message += " There are already values configured. Generating would overwrite it!";
                }
                maxAngle = Mathf.Round(EditorGUILayout.Slider("Max Angle", maxAngle, 0, 180));
                angleStep = Mathf.Round(EditorGUILayout.Slider("Angle Step", angleStep, 0, 180));
                raySelectionThreshold = EditorGUILayout.Slider("Ray selection threshold", raySelectionThreshold, 0, 0.05f);

                if (GUILayout.Button("Generate new values"))
                {
                    if (EditorUtility.DisplayDialog("Generate angles", message, "Generate", "Cancel"))
                    {
                        t.angles = HPUIInteractorFullRangeAngles.ComputeAngles((int)maxAngle, (int)angleStep, raySelectionThreshold);
                        EditorUtility.SetDirty(t);
                        serializedObject.ApplyModifiedProperties();
                    }
                    generating = false;
                }
            }
        }
    }
}
