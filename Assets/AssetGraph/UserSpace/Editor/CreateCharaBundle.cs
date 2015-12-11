using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

/**
	if you want to use AssetBundle or other kind of compression,
	Drag & Drop this C# script to AssetGraph.
*/
public class CreateCharaBundle : AssetGraph.BundlizerBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir) {
		var mainAssetInfo = source[0];
		var mainAsset = AssetDatabase.LoadAssetAtPath(mainAssetInfo.assetPath, mainAssetInfo.assetType) as UnityEngine.Object;

		var sunAssetInfos = source.GetRange(1, source.Count-1);
		var subAssets = new List<UnityEngine.Object>();

		foreach (var subAssetInfo in sunAssetInfos) {
			subAssets.Add(
				AssetDatabase.LoadAssetAtPath(subAssetInfo.assetPath, subAssetInfo.assetType) as UnityEngine.Object
			);
		}

		// create AssetBundle from assets.
		var targetPath = Path.Combine(recommendedBundleOutputDir, "chara_" + groupkey + ".assetbundle");
		
		uint crc = 0;
		try {
			BuildPipeline.BuildAssetBundle(
				mainAsset,
				subAssets.ToArray(),
				targetPath,
				out crc,
				BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
				BuildTarget.iOS
			);
		} catch (Exception e) {
			Debug.Log("failed to create AssetBundle:" + targetPath + " error:" + e);
		}
	}
}