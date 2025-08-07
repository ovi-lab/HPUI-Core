using UnityEngine;
using UnityEditor;
using ubco.ovilab.HPUI.Interaction;
using System;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System.IO;

namespace ubco.ovilab.HPUI.Editor
{
    /// <summary>
    /// Rename the children of the selected components to {childrenPrefix}{index}, index starting from start index.
    /// </summary>
    public class LoadAndSaveConeDataFromJson : EditorWindow {
        private static readonly Vector2Int size = new Vector2Int(350, 200);
        private static HPUIInteractorConeRayAngles loadedAsset;
        private static string path;

        [MenuItem("HPUI/Load cone angles data from json")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow<LoadAndSaveConeDataFromJson>();
            window.minSize = size;
            window.maxSize = size;
        }

        private void OnGUI()
        {
            if (GUILayout.Button(new GUIContent("Load file", "Save the asset.")) &&
                (loadedAsset == null || EditorUtility.DisplayDialog("Discard loaded data?", "If not saved, previously loaded data will be lost when loading new data. Proceed?", "yes", "no")))
            {
                path = EditorUtility.OpenFilePanel("Serialized cone data to load", "", "json");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    try
                    {
                        string fileContent = File.ReadAllText(path);
                        loadedAsset = JsonConvert.DeserializeObject<HPUIInteractorConeRayAngles>(fileContent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"{e}");
                        path = null;
                    }
                }
            }
            EditorGUILayout.Space();

            GUI.enabled = loadedAsset != null;
            if (GUILayout.Button(new GUIContent("Save", "Save the asset.")))
            {
                string saveName = EditorUtility.SaveFilePanelInProject("Save new cone angles asset", "NewHPUIInteractorConeRayAngles.asset", "asset", "Save location for the generated cone angle asset.");
                try
                {
                    AssetDatabase.CreateAsset(loadedAsset, saveName);
                    AssetDatabase.SaveAssets();
                }
                catch(Exception e)
                {
                    Debug.LogError($"{e}");
                }
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField("File loaded summary");
            if (loadedAsset != null)
            {
                StringBuilder sb = new();
                sb.AppendLine($"File: {path}\n");
                sb.AppendLine($"Fallback side: {loadedAsset.FallbackSide}\n");

                sb.AppendFormat("- IndexDistalAngles        : {0}\n", string.Join(",", loadedAsset.IndexDistalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- IndexIntermediateAngles  : {0}\n", string.Join(",", loadedAsset.IndexIntermediateAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- IndexProximalAngles      : {0}\n", string.Join(",", loadedAsset.IndexProximalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- MiddleDistalAngles       : {0}\n", string.Join(",", loadedAsset.MiddleDistalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- MiddleIntermediateAngles : {0}\n", string.Join(",", loadedAsset.MiddleIntermediateAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- MiddleProximalAngles     : {0}\n", string.Join(",", loadedAsset.MiddleProximalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- RingDistalAngles         : {0}\n", string.Join(",", loadedAsset.RingDistalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- RingIntermediateAngles   : {0}\n", string.Join(",", loadedAsset.RingIntermediateAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- RingProximalAngles       : {0}\n", string.Join(",", loadedAsset.RingProximalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- LittleDistalAngles       : {0}\n", string.Join(",", loadedAsset.LittleDistalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- LittleIntermediateAngles : {0}\n", string.Join(",", loadedAsset.LittleIntermediateAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                sb.AppendFormat("- LittleProximalAngles     : {0}\n", string.Join(",", loadedAsset.LittleProximalAngles.Select(s => $" {s.side}: {s.rayAngles.Count()}")));
                EditorGUILayout.TextArea(sb.ToString());
            }
            else
            {
                EditorGUILayout.TextArea("n/a");
            }
        }
    }
}
