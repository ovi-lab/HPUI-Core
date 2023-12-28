using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Core;
using UnityEditor.XR.Interaction.Toolkit;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomEditor(typeof(HPUIContinuousInteractable), true)]
    public class HPUIContinuousInteractableEditor: HPUIBaseInteractableEditor
    {
        private HPUIContinuousInteractable t;

        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIContinuousInteractable;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Run calibration"))
            {
                t.Calibrate();
            }
            GUI.enabled = true;
        }
    }
}
