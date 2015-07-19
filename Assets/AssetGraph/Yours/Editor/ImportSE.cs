using UnityEngine;
using UnityEditor;

public class ImportSE : AssetGraph.ImporterBase {
	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("ImportSE assetPath:" + assetPath);
	}
}