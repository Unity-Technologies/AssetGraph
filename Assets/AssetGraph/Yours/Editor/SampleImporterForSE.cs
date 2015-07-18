using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForSE : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("SampleImporterForSE AssetGraphOnPreprocessAudio started.");
		
		Debug.Log("SampleImporterForSE AssetGraphOnPreprocessAudio completed.");
	}
	
}