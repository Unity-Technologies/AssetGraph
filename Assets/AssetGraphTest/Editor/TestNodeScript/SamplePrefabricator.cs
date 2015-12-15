using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SamplePrefabricator : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		var notMetaAssets = sources.Where(source => !source.assetPath.EndsWith(".meta")).ToList();

		// get texture.
		var textureAssetPath = notMetaAssets[0].assetPath;
		var textureAssetType = notMetaAssets[0].assetType;

		// load texture from AssetDatabase.
		var characterTexture = AssetDatabase.LoadAssetAtPath(textureAssetPath, textureAssetType) as Texture2D;
			
		if (characterTexture) Debug.Log("Prefabricate:loaded:" + textureAssetPath);
		else Debug.LogError("Prefabricate:failed to load:" + textureAssetPath);


		// get material from path.
		var materialAssetPath = notMetaAssets[1].assetPath;
		var materialAssetType = notMetaAssets[1].assetType;

		// load texture from AssetDatabase.
		var characterMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, materialAssetType) as Material;
		
		// // then set loaded texture to that material.
		characterMaterial.mainTexture = characterTexture;

		// generate cube then set texture to it.
		var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
		meshRenderer.material = characterMaterial;

		// generate prefab in prefabBaseName folder. "node/SOMEWHERE/groupKey/prefab.prefab". AssetGraph determines this path automatically.
		var generatedPrefabPath = Prefabricate(cubeObj, "prefab.prefab", false);
		Debug.Log("prefab:" + generatedPrefabPath + " is generated or already cached.");

		// delete unnecessary cube model from hierarchy.
		GameObject.DestroyImmediate(cubeObj);
	}
}