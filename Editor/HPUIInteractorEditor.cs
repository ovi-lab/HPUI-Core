using UnityEditor;
using ubco.ovilab.HPUI.Core.Interaction;
using UnityEditor.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Core.Tracking;
using UnityEngine;

using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Core.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HPUIInteractor), true)]
    public class HPUIInteractorEditor: XRBaseInteractorEditor
    {
        private HPUIInteractor t;
        protected List<SerializedProperty> eventProperties;
        protected SerializedProperty coneRayAnglesProperty, fullRangeRayAnglesProperty;
        protected List<string> eventPropertyNames = new List<string>() { "tapEvent", "gestureEvent" };

        protected bool hpuiInteractablesExpanded;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            t = target as HPUIInteractor;

            eventProperties = new List<SerializedProperty>();
            foreach (string eventName in eventPropertyNames)
            {
                eventProperties.Add(serializedObject.FindProperty(eventName));
            }

            coneRayAnglesProperty = serializedObject.FindProperty("coneRayAngles");
            fullRangeRayAnglesProperty = serializedObject.FindProperty("fullRangeRayAngles");
        }

        /// <inheritdoc />
        protected override void DrawInspector()
        {
            base.DrawInspector();

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
        protected override void DrawDerivedProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HPUI Configurations", EditorStyles.boldLabel);
            base.DrawDerivedProperties();
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            DrawCoreConfiguration();
        }

        /// <inheritdoc />
        protected override void DrawCoreConfiguration()
        {
            DrawInteractionManagement();
            if (t.TryGetComponent<JointFollower>(out JointFollower jointFollower))
            {
                EditorGUILayout.HelpBox("Using handedness from JointFollower", MessageType.Info);
                GUI.enabled = false;
                t.handedness = jointFollower.Handedness switch {
                    Handedness.Right => InteractorHandedness.Right,
                    Handedness.Left => InteractorHandedness.Left,
                    _ => InteractorHandedness.None,
                };
                EditorGUILayout.PropertyField(m_Handedness, BaseContents.handedness);
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.PropertyField(m_Handedness, BaseContents.handedness);
            }
            EditorGUILayout.PropertyField(m_AttachTransform, BaseContents.attachTransform);
            EditorGUILayout.PropertyField(m_DisableVisualsWhenBlockedInGroup, BaseContents.disableVisualsWhenBlockedInGroup);
            EditorGUILayout.PropertyField(m_StartingSelectedInteractable, BaseContents.startingSelectedInteractable);
        }

        /// <inheritdoc />
        protected override List<string> GetDerivedSerializedPropertyNames()
        {
            List<string> props = base.GetDerivedSerializedPropertyNames();
            props.AddRange(eventPropertyNames);
            return props;
        }
    }
}
