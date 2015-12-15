using UnityEngine;
using UnityEditor;

/**
	if you want to import Assets manually,
	Drag & Drop this C# script to AssetGraph.
*/
public class ImportCharaImage : AssetGraph.ImporterBase {
	public override void AssetGraphOnPreprocessTexture () {
		UnityEditor.TextureImporter importer = assetImporter as UnityEditor.TextureImporter;
		importer.textureType			= UnityEditor.TextureImporterType.Advanced;
		importer.npotScale				= TextureImporterNPOTScale.None;
		importer.isReadable				= true;
		importer.alphaIsTransparency 	= true;
		importer.mipmapEnabled			= false;
		importer.wrapMode				= TextureWrapMode.Repeat;
		importer.filterMode				= FilterMode.Bilinear;
		importer.textureFormat 			= TextureImporterFormat.ARGB16;
	}
}