using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class SamplePrefabricator_0 : AssetGraph.PrefabricatorBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, string> Prefabricate) {
		var textureAssetPath = source[0].assetPath;
		var textureAssetType = source[0].assetType;

		// load texture from AssetDatabase.
		var characterTexture = AssetDatabase.LoadAssetAtPath(textureAssetPath, textureAssetType) as Texture2D;
		
		var newMaterialPath = Path.Combine(recommendedPrefabOutputDir, "material.mat");

		// generate texture material
		var characterMaterial = new Material(Shader.Find ("Transparent/Diffuse"));
		AssetDatabase.CreateAsset(characterMaterial, newMaterialPath);
		
		// then set loaded texture to that material.
		characterMaterial.mainTexture = characterTexture;


		// generate cube then set texture to it.
		var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
		meshRenderer.material = characterMaterial;

		// generate prefab in prefabBaseName folder."SOMEWHERE/prefab.prefab" from object.
		Prefabricate(cubeObj, "prefab.prefab");

		// delete unnecessary cube model from hierarchy.
		GameObject.DestroyImmediate(cubeObj);
	}
}