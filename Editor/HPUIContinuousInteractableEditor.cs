using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using System.Linq;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomEditor(typeof(HPUIContinuousInteractable), true)]
    public class HPUIContinuousInteractableEditor: HPUIBaseInteractableEditor
    {
        private HPUIContinuousInteractable t;
        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>() { "continuousSurfaceCreatedEvent" }).ToList();

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
                t.Configure();
            }
            GUI.enabled = true;
        }
    }
}
