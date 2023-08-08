using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ubc.ok.ovilab.HPUI.Core;

namespace ubc.ok.ovilab.HPUI.Editor
{
    [CustomEditor(typeof(InteractableButtonsRoot), true)]
    public class InteractableButtonsRootEditor: UnityEditor.Editor
    {
        private InteractableButtonsRoot t;
        private List<ButtonController> btns;

        void OnEnable()
        {
            t = target as InteractableButtonsRoot;
            btns = t.GetComponentsInChildren<ButtonController>().ToList();
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            if (GUILayout.Button("Update buttons"))
            {
                btns = t.GetComponentsInChildren<ButtonController>().ToList();
            }
            GUI.enabled = EditorApplication.isPlaying;
            EditorGUILayout.Space();
            Rect rectBox = EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Buttons", EditorStyles.boldLabel);
            foreach (ButtonController btn in btns)
            {
                EditorGUILayout.BeginHorizontal();
                string name = btn.transform.parent.parent.name;
                EditorGUILayout.PrefixLabel(name);
                if (GUILayout.Button("Trigger " + name))
                {
                    ButtonController.TriggerTargetButton(btn);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
