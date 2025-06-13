using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIInteractorPill), true)]
    public class HPUIInteractorPillEditor : UnityEditor.Editor
    {
        private HPUIInteractorPill t;
        private bool generating = false;
        private float maxAngle = 90;
        private float angleStep = 5;
        private float axisA = 0.015f;
        private float axisB = 0.015f;
        private float axisC = 0.015f;

        protected void OnEnable()
        {
            t = target as HPUIInteractorPill;
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
                if (t.angles == null)
                {
                    t.angles = new();
                }

                if (t.angles.Count != 0)
                {
                    EditorGUILayout.HelpBox("There are already values configured. Generating would overwrite it!", MessageType.Warning);
                    message += " There are already values configured. Generating would overwrite it!";
                }
                maxAngle = Mathf.Round(EditorGUILayout.Slider("Max Angle", maxAngle, 0, 180));
                angleStep = Mathf.Round(EditorGUILayout.Slider("Angle Step", angleStep, 0, 180));

                axisA = EditorGUILayout.Slider("Length along local X", axisA, 0, 0.05f);
                axisB = EditorGUILayout.Slider("Length along local Y", axisB, 0, 0.05f);
                axisC = EditorGUILayout.Slider("Length along local Z", axisC, 0, 0.05f);

                if (GUILayout.Button("Generate new values"))
                {
                    if (EditorUtility.DisplayDialog("Generate angles", message, "Generate", "Cancel"))
                    {
                        t.angles = HPUIInteractorPill.ComputeAngles((int)maxAngle, (int)angleStep, axisA, axisB, axisC);
                        EditorUtility.SetDirty(t);
                        serializedObject.ApplyModifiedProperties();
                    }
                    generating = false;
                }
            }
        }
    }
}
