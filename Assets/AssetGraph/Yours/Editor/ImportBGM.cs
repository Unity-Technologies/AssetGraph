using UnityEngine;
using UnityEditor;

public class ImportBGM : AssetGraph.ImporterBase {
	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("ImportBGM assetPath:" + assetPath);
	}	
}