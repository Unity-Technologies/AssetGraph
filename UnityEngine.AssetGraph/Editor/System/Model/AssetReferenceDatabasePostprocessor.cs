using UnityEngine;
using UnityEditor;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	class AssetReferenceDatabasePostprocessor : AssetPostprocessor 
	{
		static void OnPostprocessAllAssets (string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			LogUtility.Logger.Log("[OnPostprocessAllAssets]");

			foreach (string str in deletedAssets) 
			{
				AssetReferenceDatabase.DeleteReference(str);
			}

			for (int i=0; i<movedAssets.Length; i++)
			{
				AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
			}

			NotifyAssetPostprocessorGraphs (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

			AssetBundleGraphEditorWindow.NotifyAssetsReimportedToAllWindows(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
		}

		static void NotifyAssetPostprocessorGraphs(string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                if (graph != null && graph.UseAsAssetPostprocessor) {
					foreach(var n in graph.Nodes) {
						n.Operation.Object.OnAssetsReimported(n, null, EditorUserBuildSettings.activeBuildTarget, importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
					}
					AssetBundleGraphUtility.ExecuteGraph (graph);
				}
			}
		}
	}
}
