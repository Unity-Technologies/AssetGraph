using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
    public class JSONGraphUtility {

        public static void ExportGraphToJSONFromDialog(Model.ConfigGraph graph) {

            string path =
                EditorUtility.SaveFilePanelInProject(
                    string.Format("Export {0} to JSON file", graph.name), 
                    graph.name, "json", 
                    "Export to:");
            if(string.IsNullOrEmpty(path)) {
                return;
            }

            string jsonString = EditorJsonUtility.ToJson (graph, true);

            File.WriteAllText (path, jsonString, System.Text.Encoding.UTF8);
		}

        public static void ExportAllGraphsToJSONFromDialog() {

            var folderSelected = 
                EditorUtility.OpenFolderPanel("Select folder to export all graphs", Application.dataPath + "..", "");
            if(string.IsNullOrEmpty(folderSelected)) {
                return;
            }

            var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            foreach(var guid in guids) {
                string graphPath = AssetDatabase.GUIDToAssetPath(guid);
                string graphName = Path.GetFileNameWithoutExtension(graphPath);

                string jsonFilePath = Path.Combine (folderSelected, string.Format("{0}.json", graphName));

                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphPath);
                string jsonString = EditorJsonUtility.ToJson (graph, true);

                File.WriteAllText (jsonFilePath, jsonString, System.Text.Encoding.UTF8);
            }
        }

        public static Model.ConfigGraph ImportJSONToGraphFromDialog(Model.ConfigGraph graph) {

            string fileSelected = EditorUtility.OpenFilePanelWithFilters("Select JSON files to import", Application.dataPath, new string[] {"JSON files", "json", "All files", "*"});
            if(string.IsNullOrEmpty(fileSelected)) {
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(fileSelected);

            var jsonContent = File.ReadAllText (fileSelected, System.Text.Encoding.UTF8);

            if (graph != null) {
                Undo.RecordObject(graph, "Import");
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
            } else {
                graph = ScriptableObject.CreateInstance<Model.ConfigGraph>();
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
                var newAssetFolder = CreateFolderForImportedAssets ();
                var graphPath = FileUtility.PathCombine(newAssetFolder, string.Format("{0}.asset", name));
                AssetDatabase.CreateAsset (graph, graphPath);
            }
            return graph;
        }

        public static void ImportAllJSONInDirectoryToGraphFromDialog() {
            var folderSelected = 
                EditorUtility.OpenFolderPanel("Select folder contains JSON files to import", Application.dataPath + "..", "");
            if(string.IsNullOrEmpty(folderSelected)) {
                return;
            }

            var newAssetFolder = CreateFolderForImportedAssets ();

            var filePaths = FileUtility.GetAllFilePathsInFolder (folderSelected);
            foreach (var path in filePaths) {
                var ext = Path.GetExtension (path).ToLower ();
                if (ext != ".json") {
                    continue;
                }
                var jsonContent = File.ReadAllText (path, System.Text.Encoding.UTF8);
                var name = Path.GetFileNameWithoutExtension (path);

                var graph = ScriptableObject.CreateInstance<Model.ConfigGraph>();
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
                var graphPath = FileUtility.PathCombine(newAssetFolder, string.Format("{0}.asset", name));
                AssetDatabase.CreateAsset (graph, graphPath);
            }
        }

        private static string CreateFolderForImportedAssets() {
            var t = DateTime.Now;
            var folderName = String.Format ("ImportedGraphs_{0:D4}-{1:D2}_{2:D2}_{3:D2}{4:D2}{5:D2}", t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

            AssetDatabase.CreateFolder ("Assets", folderName);

            return String.Format("Assets/{0}", folderName);
        }
	}
}
