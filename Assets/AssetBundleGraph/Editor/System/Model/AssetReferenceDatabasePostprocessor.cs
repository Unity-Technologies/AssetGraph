using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph {
	class AssetReferenceDatabasePostprocessor : AssetPostprocessor 
	{
		static void OnPostprocessAllAssets (string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			LogUtility.Logger.Log("[OnPostprocessAllAssets]");
			AssetBundleGraphEditorWindow.OnAssetsReimported(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

			foreach (string str in deletedAssets) 
			{
				LogUtility.Logger.Log("Deleted Asset: " + str);
				AssetReferenceDatabase.DeleteReference(str);
			}

			for (int i=0; i<movedAssets.Length; i++)
			{
				LogUtility.Logger.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
				AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
			}
		}
	}
}
