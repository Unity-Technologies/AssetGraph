using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class SamplePrefabricator_3 : AssetGraph.PrefabricatorBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir) {
		Debug.Log("SamplePrefabricator_3");

		// 複数のセットを作りたい場合、その流入自体は全体の素材が一気に来るので、グルーピングするための情報が必要になる。
		// 「こんな素材が来るはずなのでそれのなかで、この素材一式を1セットとして扱う」っていうのを自動化する術が無い。人間がやるしか無い。

		// ここでは 素材のパスにグループ化の要素 "chara" + 数字 0~n で扱う。
		// 素材のidとかで扱ってもいいとは思うんだけど、けっこう手段が限られそう。
		
		// ここでの n はどうやって設定すればいいんだろう、、つらいな、、、2点問題がある
		// ・たくさんある素材からセットを作るわけだが、その上限値がわからん
		// 		-> 下記の例ではsource全体 = 全てのグループを仮の総量として設定しているが、まあ理屈で言えばPrefab数 < 素材数 だと思うのだけれど、かなり変態っぽい。
		// ・番号を使う場合、連番を強制される
		// 		-> 例えば0~100まではつくってあって、101~200をつくる、とかの時に、問題がでる。

		// ベストプラクティスとして、・素材の名称にIDを入れる、というのがある。ここではやってないけど、テストの0_14番あたりでやってみよう。

		/*
			フォルダ名に入っている"chara"というキーワードを元に複数の素材からセットを作り出し、
			モデルにテクスチャを貼ってそれをPrefabにする。
			PrefabはrecommendedPrefabOutputDir/charaN/prefab.prefabとして吐き出す。
		*/
		for (var i = 0; i < source.Count; i++) {
			var imageAssets = source.Where(s => s.assetPath.Contains("/images/chara" + i + "/")).ToList();
			if (!imageAssets.Any()) continue;// この素材が存在しなければ後続も無い

			var modelAssets = source.Where(s => s.assetPath.Contains("/models/chara" + i + "/")).ToList();
			var materialAssets = source.Where(s => s.assetPath.Contains("/models/chara" + i + "/Materials/")).ToList();
			// Debug.LogError("通過:" + i);
			
			var imageAsset = imageAssets[0];
			var modelAsset = modelAssets[0];
			var materialAsset = materialAssets[0];
			// Debug.LogError("imageAsset:" + imageAsset);
			// Debug.LogError("modelAsset:" + modelAsset);
			// Debug.LogError("materialAsset:" + materialAsset);
			
			var modelMaterial = AssetDatabase.LoadAssetAtPath(materialAsset.assetPath, materialAsset.assetType) as Material;
			// Debug.LogError("modelMaterial:" + modelMaterial);

			var textureImage = AssetDatabase.LoadAssetAtPath(imageAsset.assetPath, imageAsset.assetType) as Texture2D;

			// // then set loaded texture to that material.
			modelMaterial.mainTexture = textureImage;

			// んでこのMaterialをModelにセットしてプレファブ化するよな
			var model = AssetDatabase.LoadAssetAtPath(modelAsset.assetPath, modelAsset.assetType) as GameObject;
			
			// Instantiate.
			var modelObj = GameObject.Instantiate(model);

			var meshRenderer = modelObj.GetComponentInChildren<MeshRenderer>();
			// Debug.LogError("meshRenderer:" + meshRenderer);
			meshRenderer.material = modelMaterial;

			// generate prefab in prefabBaseName folder."SOMEWHERE/example";
			var newPrefabOutputBasePath = Path.Combine(recommendedPrefabOutputDir, "chara" + i);

			// create "recommendedPrefabOutputDir"/charaN/ folder before creating prefab.
			Directory.CreateDirectory(newPrefabOutputBasePath);
			
			var newPrefabOutputPath = Path.Combine(newPrefabOutputBasePath, "chara" + i + "_prefab.prefab");
			UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(newPrefabOutputPath);
			
			// export prefab data.
			PrefabUtility.ReplacePrefab(modelObj, prefabFile);

			GameObject.DestroyImmediate(modelObj);
		}
	}
}