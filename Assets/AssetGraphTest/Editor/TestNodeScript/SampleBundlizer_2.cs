using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SampleBundlizer_2 : AssetGraph.BundlizerBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir) {

		// フォルダ名からセットを構築する。SamplePrefabricator_3 と同一の扱い。

		/*
			Model, Prefab, Image, Material, BGM, SE をまとめて一つずつのAssetBundleにする
		*/
		for (var i = 0; i < source.Count; i++) {
			var charaAssets = source.Where(s => s.assetPath.Contains("/chara" + i + "/")).ToList();
			if (!charaAssets.Any()) return;

			UnityEngine.Object mainAsset = null;
			var subAssets = new List<UnityEngine.Object>();
			
			for (int j = 0; j < charaAssets.Count; j++) {
				var targetAssetInfo = charaAssets[j];

				var targetAsset = AssetDatabase.LoadAssetAtPath(targetAssetInfo.assetPath, targetAssetInfo.assetType) as UnityEngine.Object;
				// Debug.LogError("targetAsset:" + targetAsset);

				if (j == 0) {
					mainAsset = targetAsset;
				}

				if (0 < j) {
					subAssets.Add(targetAsset);
				}
			}

			/**
				generate AssetBundle for iOS from mainAsset & subAssets.
			*/
			{
				uint crc = 0;	
				var targetBasePath = Path.Combine(recommendedBundleOutputDir, "chara" + i);

				// create directory.
				Directory.CreateDirectory(targetBasePath);

				var targetPath = Path.Combine(targetBasePath, "chara" + i + ".assetbundle");
				
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
					Debug.Log("SampleBundlizer_2:e:" + e);
				}
			}
		}
	}
}