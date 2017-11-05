using UnityEngine;
using UnityEditor;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	class AssetReferenceDatabasePostprocessor : AssetPostprocessor 
	{
		static void OnPostprocessAllAssets (string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
            foreach (string movedFrom in movedFromAssetPaths) {
                if (movedFrom == AssetGraphBasePath.BasePath) {
                    AssetGraphBasePath.ResetBasePath ();
                }
            }

			foreach (string str in deletedAssets) 
			{
				AssetReferenceDatabase.DeleteReference(str);
			}

			for (int i=0; i<movedAssets.Length; i++)
			{
				AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
			}

			NotifyAssetPostprocessorGraphs (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

			AssetGraphEditorWindow.NotifyAssetsReimportedToAllWindows(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
		}

		static void NotifyAssetPostprocessorGraphs(string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                if (graph != null && graph.UseAsAssetPostprocessor) {
                    bool isAnyNodeAffected = false;
					foreach(var n in graph.Nodes) {
                        isAnyNodeAffected |= n.Operation.Object.OnAssetsReimported(n, null, EditorUserBuildSettings.activeBuildTarget, importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
					}
                    if (isAnyNodeAffected) {
                        AssetGraphUtility.ExecuteGraph (graph);
                    }
				}
			}
		}
	}
}
