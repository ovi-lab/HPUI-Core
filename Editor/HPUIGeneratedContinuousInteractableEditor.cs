using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using System.Linq;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIGeneratedContinuousInteractable), true)]
    public class HPUIGeneratedContinuousInteractableEditor: HPUIBaseInteractableEditor
    {
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Assets/HPUIContinousUI.prefab";
        private HPUIGeneratedContinuousInteractable t;
        private SerializedProperty uiProp;

        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>()
        {
            "continuousSurfaceCreatedEvent",
            "boundsCollider", // NOTE: this is not relevant to the HPUIGeneratedContinuousInteractable.
        }).ToList();

        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIGeneratedContinuousInteractable;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button(new GUIContent("Manual recompute", "Assuming the the corresponding hand is held straight, compute and generate the surface")))
            {
                foreach (Object t in targets)
                {
                    (t as HPUIGeneratedContinuousInteractable)?.ManualRecompute();
                }
            }
            GUI.enabled = true;
        }
    }
}
