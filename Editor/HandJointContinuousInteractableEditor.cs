using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Core;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomEditor(typeof(HandJointContinuousInteractable), true)]
    public class HandJointContinuousInteractableEditor: UnityEditor.Editor
    {
        private HandJointContinuousInteractable t;

        void OnEnable()
        {
            t = target as HandJointContinuousInteractable;
        }

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Run calibration"))
            {
                t.Calibrate();
            }
            GUI.enabled = true;
        }
    }
}
