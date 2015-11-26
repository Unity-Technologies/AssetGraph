using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class SamplePrefabricator_3 : AssetGraph.PrefabricatorBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		/*
			フォルダ名に入っている"chara"というキーワードを元に複数の素材からセットを作り出し、
			モデルにテクスチャを貼ってそれをPrefabにする。
			PrefabはrecommendedPrefabOutputDir/charaN/prefab.prefabとして吐き出す。
		*/
		for (var i = 0; i < source.Count; i++) {
			var imageAssets = source.Where(s => s.assetPath.Contains("/images/chara" + i + "/")).ToList();
			if (!imageAssets.Any()) continue;// この素材が存在しなければ後続も無い

			var modelAssets = source.Where(s => s.assetPath.Contains("/models/chara" + i + "/") && s.assetPath.EndsWith(".fbx")).ToList();
			var materialAssets = source.Where(s => s.assetPath.Contains("/models/chara" + i + "/Materials/")).ToList();

			
			var imageAsset = imageAssets[0];
			var modelAsset = modelAssets[0];
			var materialAsset = materialAssets[0];
			// Debug.LogError("imageAsset:" + imageAsset);
			// Debug.LogError("modelAsset:" + modelAsset);
			// Debug.LogError("materialAsset:" + materialAsset);
			
			var modelMaterial = AssetDatabase.LoadAssetAtPath(materialAsset.assetPath, materialAsset.assetType) as Material;
			// Debug.LogError("modelMaterial:" + modelMaterial);

			var textureImage = AssetDatabase.LoadAssetAtPath(imageAsset.assetPath, imageAsset.assetType) as Texture2D;
			// Debug.LogError("textureImage:" + textureImage);

			// then set loaded texture to that material.
			modelMaterial.mainTexture = textureImage;

			var model = AssetDatabase.LoadAssetAtPath(modelAsset.assetPath, modelAsset.assetType) as GameObject;

			// Instantiate.
			var modelObj = GameObject.Instantiate(model);

			var meshRenderer = modelObj.GetComponentInChildren<MeshRenderer>();
			// Debug.LogError("meshRenderer:" + meshRenderer);
			meshRenderer.material = modelMaterial;

			Prefabricate(modelObj, "chara" + i + "_prefab.prefab", false);

			GameObject.DestroyImmediate(modelObj);
		}
	}
}