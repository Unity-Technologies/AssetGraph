using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph {
	class AssetReferenceDatabasePostprocessor : AssetPostprocessor 
	{
		static void OnPostprocessAllAssets (string[] importedAssets, 
			string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
		{
			Debug.Log("[OnPostprocessAllAssets]");
			AssetBundleGraphEditorWindow.OnAssetsReimported(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);

			foreach (string str in deletedAssets) 
			{
				Debug.Log("Deleted Asset: " + str);
				AssetReferenceDatabase.DeleteReference(str);
			}

			for (int i=0; i<movedAssets.Length; i++)
			{
				Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
				AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
			}
		}
	}
}
