using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using UnityEditor.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using UnityEngine;
using ubco.ovilab.HPUI.Tracking;

namespace ubco.ovilab.HPUI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIBaseInteractable), true)]
    public class HPUIBaseInteractableEditor: XRBaseInteractableEditor
    {
        private HPUIBaseInteractable t;
        protected List<SerializedProperty> eventProperties;
        protected virtual List<string> EventPropertyNames => new List<string>() { "tapEvent", "gestureEvent" };

        protected bool hpuiInteractablesExpanded;
        protected SerializedProperty handednessProperty;
        protected SerializedProperty boundsColliderProperty;
        protected SerializedProperty zOrderProperty;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIBaseInteractable;

            eventProperties = new List<SerializedProperty>();
            foreach (string eventName in EventPropertyNames)
            {
                eventProperties.Add(serializedObject.FindProperty(eventName));
            }

            handednessProperty = serializedObject.FindProperty("handedness");
            boundsColliderProperty = serializedObject.FindProperty("boundsCollider");
            zOrderProperty = serializedObject.FindProperty("_zOrder");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HPUI Configurations", EditorStyles.boldLabel);
            if (t.TryGetComponent<JointFollower>(out JointFollower jointFollower))
            {
                EditorGUILayout.HelpBox("Using handedness from JointFollower", MessageType.Info);
                GUI.enabled = false;
                t.Handedness = jointFollower.Handedness;
                EditorGUILayout.PropertyField(handednessProperty);
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.PropertyField(handednessProperty);
            }
            EditorGUILayout.PropertyField(boundsColliderProperty);
            EditorGUILayout.PropertyField(zOrderProperty);
        }

        /// <inheritdoc />
        protected override void DrawEvents()
        {
            base.DrawEvents();

            EditorGUILayout.Space();

            hpuiInteractablesExpanded = EditorGUILayout.Foldout(hpuiInteractablesExpanded, EditorGUIUtility.TrTempContent("HPUI Events"), true);
            if (hpuiInteractablesExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (SerializedProperty property in eventProperties)
                    {
                        EditorGUILayout.PropertyField(property);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override List<string> GetDerivedSerializedPropertyNames()
        {
            List<string> props = base.GetDerivedSerializedPropertyNames();
            props.AddRange(EventPropertyNames);
            props.Add("handedness");
            props.Add("boundsCollider");
            props.Add("_zOrder");
            return props;
        }

        // Selection mode and distance calculation mode are programatically set.
        /// <inheritdoc />
        protected override void DrawSelectionConfiguration() { }

        /// <inheritdoc />
        protected override void DrawDistanceCalculationMode() { }
    }
}
