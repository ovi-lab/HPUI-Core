using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI.UI;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIContinuousInteractable), true)]
    public class HPUIContinuousInteractableEditor: HPUIBaseInteractableEditor
    {
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Assets/HPUIContinousUI.prefab";
        private HPUIContinuousInteractable t;
        private SerializedProperty uiProp;

        protected override List<string> EventPropertyNames => base.EventPropertyNames.Union(new List<string>()
        {
            "continuousSurfaceCreatedEvent",
            "boundsCollider", // NOTE: this is not relevant to the HPUIContinuousInteractable.
            "ui"
        }).ToList();

        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIContinuousInteractable;
            uiProp = serializedObject.FindProperty("ui");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button(new GUIContent("Manual recompute", "Assuming the the corresponding hand is held straight, compute and generate the surface")))
            {
                foreach (Object t in targets)
                {
                    (t as HPUIContinuousInteractable)?.ManualRecompute();
                }
            }
            GUI.enabled = true;
        }
    }
}
