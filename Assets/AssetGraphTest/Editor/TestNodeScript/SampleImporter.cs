using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForImage : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessTexture () {
		Debug.Log("SampleImporterForImage AssetGraphOnPreprocessTexture started.");
		
		UnityEditor.TextureImporter importer = assetImporter as UnityEditor.TextureImporter;
		importer.textureType			= UnityEditor.TextureImporterType.Advanced;
		importer.npotScale				= TextureImporterNPOTScale.None;
		importer.isReadable				= true;
		importer.alphaIsTransparency 	= true;
		importer.mipmapEnabled			= false;
		importer.wrapMode				= TextureWrapMode.Repeat;
		importer.filterMode				= FilterMode.Bilinear;
		importer.textureFormat 			= TextureImporterFormat.ARGB16;

		Debug.Log("SampleImporterForImage AssetGraphOnPreprocessTexture completed.");
	}
	
}