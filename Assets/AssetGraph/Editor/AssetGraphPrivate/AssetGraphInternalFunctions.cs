using UnityEngine;
using UnityEditor;

using System;

namespace AssetGraph {
	public class AssetGraphInternalFunctions {
		public static Type GetAssetType (string assetPath) {
			// Debug.LogWarning("Assetの型を取得するためにDatabaseからAssets/~で取得してるんだけど、もっといい方法Internalに無いすかね。AssetIdとPathは持ってるんで使用可能。");
			var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (asset == null) throw new Exception("failed to load asset from:" + assetPath);
			return asset.GetType();
		}
	}	
}