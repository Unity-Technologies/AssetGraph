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

			AssetBundleGraphEditorWindow.NotifyAssetsReimportedToAllWindows(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
		}
	}
}
