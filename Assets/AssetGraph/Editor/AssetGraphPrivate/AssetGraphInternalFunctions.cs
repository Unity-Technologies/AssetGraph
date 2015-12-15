using UnityEngine;
using UnityEditor;

using System;

namespace AssetGraph {
	public class AssetGraphInternalFunctions {
		public static Type GetAssetType (string assetPath) {
			if (assetPath.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION)) return typeof(string);

			var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (asset == null) throw new Exception("failed to load asset from:" + assetPath);
			return asset.GetType();
		}
	}	
}