using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class CreateCharaPrefab : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
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
		foreach (var assetInfo in source) {
			if (assetInfo.assetType == typeof(Texture2D)) charaTex = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as Texture2D;
			if (assetInfo.assetType == typeof(Material)) charaMat = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as Material;
			if (assetInfo.assetType == typeof(GameObject)) charaModel = AssetDatabase.LoadAssetAtPath(assetInfo.assetPath, assetInfo.assetType) as GameObject;
		}

		// Debug.Log("charaTex:" + charaTex + "/");
		// Debug.Log("charaMat:" + charaMat + "/");
		// Debug.Log("charaModel:" + charaModel + "/");

		// set texture to material
		charaMat.mainTexture = charaTex;

		var modelObj = GameObject.Instantiate(charaModel);

		var meshRenderer = modelObj.GetComponentInChildren<MeshRenderer>();
		meshRenderer.material = charaMat;
		
		// create prefab replacable file.
		var prefabName = groupKey + "_chara.prefab";
		var prefabOutputPath = Prefabricate(modelObj, prefabName, false);

		// delete unnecessary chara model from hierarchy.
		GameObject.DestroyImmediate(modelObj);
		if (!File.Exists(prefabOutputPath)) Debug.LogError("failed to create prefab:" + prefabOutputPath);
	}
}