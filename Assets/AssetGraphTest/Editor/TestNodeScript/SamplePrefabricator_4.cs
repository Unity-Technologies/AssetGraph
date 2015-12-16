using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SamplePrefabricator_4 : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		var sourcesWithoutMeta = sources.Where(assetInfo => !assetInfo.assetPath.EndsWith(".meta")).ToList();

		// get texture.
		var textureAssetPath = sourcesWithoutMeta[2].assetPath;
		var textureAssetType = sourcesWithoutMeta[2].assetType;

		// load texture from AssetDatabase.
		var characterTexture = AssetDatabase.LoadAssetAtPath(textureAssetPath, textureAssetType) as Texture2D;
			
		if (characterTexture) Debug.Log("Prefabricate:loaded:" + textureAssetPath);
		else Debug.LogError("Prefabricate:failed to load:" + textureAssetPath);


		// get material from path.
		var materialAssetPath = sourcesWithoutMeta[0].assetPath;
		var materialAssetType = sourcesWithoutMeta[0].assetType;

		// load texture from AssetDatabase.
		var characterMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, materialAssetType) as Material;
		
		// // then set loaded texture to that material.
		characterMaterial.mainTexture = characterTexture;

		// generate cube then set texture to it.
		var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
		meshRenderer.material = characterMaterial;

		// generate prefab in prefabBaseName folder."SOMEWHERE/prefab.prefab" made from "cubeObj".
		var generatedPrefabPath = Prefabricate(cubeObj, "prefab.prefab", false);
		Debug.Log("prefab:" + generatedPrefabPath + " is generated.");

		// delete unnecessary cube model from hierarchy.
		GameObject.DestroyImmediate(cubeObj);
	}
}