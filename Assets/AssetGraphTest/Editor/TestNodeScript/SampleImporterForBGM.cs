using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForBGM : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("SampleImporterForBGM AssetGraphOnPreprocessAudio started.");
		
		Debug.Log("SampleImporterForBGM AssetGraphOnPreprocessAudio completed.");
	}
	
}