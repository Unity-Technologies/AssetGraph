using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class CreateCharaPrefab : AssetGraph.PrefabricatorBase {
	public override void In (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir) {
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


			/*
				create character's prefab.

				1.texture & material -> set texture to the material of model.
				2.model -> instantiate, then set material to model.
				3.new prefab -> prefabricate model to new prefab.
				4.delete model instance from hierarchy.
			*/

			Texture2D charaTex = null;
			Material charaMat = null;
			GameObject charaModel = null;
			foreach (var assetInfo in currentCharaAssets) {
				if (assetInfo.assetType == typeof(Texture2D)) charaTex = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as Texture2D;
				if (assetInfo.assetType == typeof(Material)) charaMat = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as Material;
				if (assetInfo.assetType == typeof(GameObject)) charaModel = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as GameObject;
			}

			Debug.Log("charaTex:" + charaTex + "/");
			Debug.Log("charaMat:" + charaMat + "/");
			Debug.Log("charaModel:" + charaModel + "/");

			// set texture to material
			charaMat.mainTexture = charaTex;

			var modelObj = GameObject.Instantiate(charaModel);

			var meshRenderer = modelObj.GetComponentInChildren<MeshRenderer>();
			meshRenderer.material = charaMat;

			// create directory for prefab.
			var targetPrefabBasePath = Path.Combine(recommendedPrefabOutputDir, "ID_" + i + "/");
			Directory.CreateDirectory(targetPrefabBasePath);

			// create prefab replacable file.
			var prefabOutputPath = Path.Combine(targetPrefabBasePath, "chara.prefab");
			var prefabFile = PrefabUtility.CreateEmptyPrefab(prefabOutputPath);
			
			// export prefab data.
			PrefabUtility.ReplacePrefab(modelObj, prefabFile);

			// delete unnecessary chara model from hierarchy.
			GameObject.DestroyImmediate(modelObj);
			Debug.Log("succeeded to create prefab:" + prefabFile);
		}
		
	}
}