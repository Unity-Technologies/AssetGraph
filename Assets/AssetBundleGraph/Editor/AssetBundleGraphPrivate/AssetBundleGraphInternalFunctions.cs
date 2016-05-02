using UnityEngine;
using UnityEditor;

using System;

namespace AssetBundleGraph {
	public class AssetBundleGraphInternalFunctions {
		public static Type GetAssetType (string assetPath) {
			if (assetPath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return typeof(string);

			var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (asset == null) throw new Exception("failed to load asset from:" + assetPath);
			return asset.GetType();
		}
	}	
}