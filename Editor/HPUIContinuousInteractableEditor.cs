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

        /// <inheritdoc />
        protected override void DrawInspector()
        {
            base.DrawInspector();
            EditorGUILayout.PropertyField(uiProp);
            if (uiProp.objectReferenceValue == null)
            {
                if (GUILayout.Button(new GUIContent("Instantiate & add UI", "Instantiate UI for continuous interface generation and add it")))
                {
                    GameObject uiObj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefab)) as GameObject;
                    uiProp.objectReferenceValue = uiObj.GetComponent<HPUIContinuousInteractableUI>();
                    Debug.Log($"[[[{uiProp.objectReferenceValue}]]]");
                }
            }
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
            if (GUILayout.Button(new GUIContent("Automated recompute", "Intiate the process to estimate the joint locations and generate the surface")))
            {
                foreach (Object t in targets)
                {
                    (t as HPUIContinuousInteractable)?.AutomatedRecompute();
                }
            }
            GUI.enabled = true;
        }
    }
}
