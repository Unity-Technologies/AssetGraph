using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SamplePrefabricator2 : AssetBundleGraph.PrefabricatorBase {
	public override void ValidateCanCreatePrefab (string nodeName, string nodeId, string groupKey, List<AssetBundleGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
		if( sources.Count < 4 ) {
			throw new AssetBundleGraph.NodeException("SamplePrefabricator2 needs at least 4 assets to create Prefab.", nodeId);
		}
		if( sources[1].assetType != typeof(Texture2D) ) {
			throw new AssetBundleGraph.NodeException("First asset is not Texture.", nodeId);
		}
		if( sources[3].assetType != typeof(Material) ) {
			throw new AssetBundleGraph.NodeException("Third asset is not Material.", nodeId);
		}
		Prefabricate("prefab2.prefab");
	}
	
	public override void CreatePrefab (string nodeName, string nodeId, string groupKey, List<AssetBundleGraph.AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		
		/*
			you can see what incoming to this Prefabricator at the Inspector between Prefabricator to next node.
			these codes are based on that information.
		*/

		if( sources.Count < 4 ) {
			throw new AssetBundleGraph.NodeException("SamplePrefabricator2 needs at least 4 assets to create Prefab.", nodeId);
		}

		if( sources[1].assetType != typeof(Texture2D) ) {
			throw new AssetBundleGraph.NodeException("Second asset is not Texture.", nodeId);
		}

		// get texture.
		var textureAssetPath = sources[1].assetPath;
		var textureAssetType = sources[1].assetType;

		// load texture from AssetDatabase.
		var characterTexture = AssetDatabase.LoadAssetAtPath(textureAssetPath, textureAssetType) as Texture2D;
			
		if (characterTexture) {
			Debug.Log("SamplePrefabricator2 loaded " + textureAssetPath);
		}
		else {
			Debug.LogError("SamplePrefabricator2 failed to load " + textureAssetPath);
		}


		if( sources[3].assetType != typeof(Material) ) {
			throw new AssetBundleGraph.NodeException("4th asset is not Material.", nodeId);
		}

		// get material from path.
		var materialAssetPath = sources[3].assetPath;
		var materialAssetType = sources[3].assetType;

		// load texture from AssetDatabase.
		var characterMaterial = AssetDatabase.LoadAssetAtPath(materialAssetPath, materialAssetType) as Material;

		if (characterMaterial) {
			Debug.Log("SamplePrefabricator2 loaded: " + materialAssetPath);
		}
		else {
			Debug.LogError("SamplePrefabricator2 failed to load: " + materialAssetPath);
		}
		
		// then set loaded texture to that material.
		characterMaterial.mainTexture = characterTexture;

		// generate cube then set texture to it.
		var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

		var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
		meshRenderer.material = characterMaterial;

		// generate prefab in prefabBaseName folder. "node/SOMEWHERE/groupKey/prefab2.prefab". AssetBundleGraph determines this path automatically.
		Prefabricate(cubeObj, "prefab2.prefab", false);

		// delete unnecessary cube model from hierarchy.
		GameObject.DestroyImmediate(cubeObj);
	}
}