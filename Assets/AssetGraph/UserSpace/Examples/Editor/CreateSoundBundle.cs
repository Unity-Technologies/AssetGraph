using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

/**
	if you want to use AssetBundle or other kind of compression,
	Drag & Drop this C# script to AssetGraph.
*/
public class CreateSoundBundle : AssetGraph.BundlizerBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir) {

		/*
			create one AssetBundle per groupKey.
		*/
		var assets = new List<UnityEngine.Object>();
		foreach (var assetInfo in source) {
			assets.Add(
				AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as UnityEngine.Object
			);
		}

		var mainAsset = assets[0];
		var subAssets = assets.GetRange(1, assets.Count - 1);

		// create AssetBundle from assets.
		var targetPath = Path.Combine(recommendedBundleOutputDir, "sounds.assetbundle");
		
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
			Debug.Log("error:" + e + " /failed to create AssetBundle:" + targetPath);
		}
	}
}