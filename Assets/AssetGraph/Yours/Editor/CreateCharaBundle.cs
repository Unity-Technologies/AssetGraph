using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class CreateCharaBundle : AssetGraph.BundlizerBase {
	public override void In (List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir) {
		var maxCount = source.Count;
		for (int i = 0; i < maxCount; i++) {
			var searchIdStr = "/ID_" + i + "/";// /ID_0~N/

			var currentCharaAssets = new List<AssetGraph.AssetInfo>();
			foreach (var assetInfo in source) {
				// Assets/AssetGraph/Temp/Imported/モデルを読み込む/models/ID_0/Materials/kiosk_0001.mat
				if (assetInfo.assetPath.Contains(searchIdStr)) {
					currentCharaAssets.Add(assetInfo);
				}
			}

			if (currentCharaAssets.Count == 0) continue;

			Debug.Log("currentCharaAssets.Count:" + currentCharaAssets.Count);

			var mainAssetInfo = currentCharaAssets[0];
			var mainAsset = AssetDatabase.LoadAssetAtPath(mainAssetInfo.assetPath, mainAssetInfo.assetType) as UnityEngine.Object;

			var sunAssetInfos = currentCharaAssets.GetRange(1, currentCharaAssets.Count-1);
			var subAssets = new List<UnityEngine.Object>();

			foreach (var subAssetInfo in sunAssetInfos) {
				subAssets.Add(
					AssetDatabase.LoadAssetAtPath(subAssetInfo.assetPath, subAssetInfo.assetType) as UnityEngine.Object
				);
			}

			// create AssetBundle from assets.
			var targetPath = Path.Combine(recommendedBundleOutputDir, "chara_" + i + ".assetbundle");
			
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
				Debug.Log("succeeded to create AssetBundle:" + targetPath);
			} catch (Exception e) {
				Debug.Log("failed to create AssetBundle:" + targetPath);
			}
		}
	}
}