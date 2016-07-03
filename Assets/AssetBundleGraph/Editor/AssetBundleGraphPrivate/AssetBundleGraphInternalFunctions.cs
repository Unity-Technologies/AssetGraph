using UnityEditor;

using System;

namespace AssetBundleGraph {
	public class AssetBundleGraphInternalFunctions {
		public static Type GetAssetType (string assetPath) {
			if (assetPath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return typeof(string);

			var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

			// if asset is null, this asset is not yet imported or denied by file extension.
			// forcely set that type to "object".
			if (asset == null) return typeof(object);
			return asset.GetType();
		}
	}	
}