using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SamplePrefabricator : AssetBundleGraph.PrefabricatorBase {
	
	public override void ValidateCanCreatePrefab (string nodeName, string nodeId, string groupKey, List<AssetBundleGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
		if( sources.Count < 3 ) {
			throw new AssetBundleGraph.NodeException("SamplePrefabricator needs at least 3 assets to create Prefab.", nodeId);
		}
		if( sources[0].assetType != typeof(Texture2D) ) {
			throw new AssetBundleGraph.NodeException("First asset is not Texture.", nodeId);
		}
		if( sources[2].assetType != typeof(Material) ) {
			throw new AssetBundleGraph.NodeException("Third asset is not Material.", nodeId);
		}
		Prefabricate("prefab.prefab");
	}
	
	public override void CreatePrefab (string nodeName, string nodeId, string groupKey, List<AssetBundleGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {

		/*
			you can see what incoming to this Prefabricator at the Inspector between Prefabricator to next node.
			these codes are based on that information.
		*/

		if( sources.Count < 3 ) {
			throw new AssetBundleGraph.NodeException("Not enough assets are given to create Prefab.", nodeId);
		}
		if( sources[0].assetType != typeof(Texture2D) ) {
			throw new AssetBundleGraph.NodeException("First asset is not Texture.", nodeId);
		}
		if( sources[2].assetType != typeof(Material) ) {
			throw new AssetBundleGraph.NodeException("Third asset is not Material.", nodeId);
		}

		// get texture.
		var textureAssetPath = sources[0].assetPath;
		var textureAssetType = sources[0].assetType;

		// load texture from AssetDatabase.
		var characterTexture = AssetDatabase.LoadAssetAtPath(textureAssetPath, textureAssetType) as Texture2D;
		
		if (characterTexture) {
			Debug.Log("SamplePrefabricator loaded " + textureAssetPath);
		}
		else {
			Debug.LogError("SamplePrefabricator failed to load " + textureAssetPath);
		}


		// get material from path.
		var materialAssetPath = sources[2].assetPath;
		var materialAssetType = sources[2].assetType;

		// load texture from AssetDatabase.
		var characterMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, materialAssetType) as Material;

		if (characterMaterial) {
			Debug.Log("SamplePrefabricator loaded " + characterMaterial);
		}
		else {
			Debug.LogError("SamplePrefabricator failed to load " + characterMaterial);
		}


		// then set loaded texture to that material.
		characterMaterial.mainTexture = characterTexture;

		// generate cube then set texture to it.
		var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
		meshRenderer.material = characterMaterial;

		// generate prefab in prefabBaseName folder. "node/SOMEWHERE/groupKey/prefab.prefab". AssetBundleGraph determines this path automatically.
		Prefabricate(cubeObj, "prefab.prefab", false);

		// delete unnecessary cube model from hierarchy.
		GameObject.DestroyImmediate(cubeObj);
	}
}