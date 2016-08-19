using UnityEditor;

using System;

namespace AssetBundleGraph {
	public class AssetBundleGraphInternalFunctions {
		public static Type GetAssetType (string assetPath) {
			if (assetPath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return typeof(string);

			var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

			// If asset is null, this asset is not imported yet, or unsupported type of file
			// so we set this to object type.
			if (asset == null) {
				return typeof(object);
			}
			return asset.GetType();
		}
	}	
}