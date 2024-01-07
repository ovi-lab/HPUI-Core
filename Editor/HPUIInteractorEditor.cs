using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using UnityEditor.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace ubco.ovilab.HPUI.Editor
{
    [CustomEditor(typeof(HPUIInteractor), true)]
    public class HPUIInteractorEditor: XRBaseInteractorEditor
    {
        private HPUIInteractor t;
        protected List<SerializedProperty> eventProperties;
        protected List<string> eventPropertyNames = new List<string>() { "tapEvent", "swipeEvent"};

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
        protected override void DrawProperties()
        {
            DrawCoreConfiguration();
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
