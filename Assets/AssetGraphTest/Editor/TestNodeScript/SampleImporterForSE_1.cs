using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForSE_1 : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("SampleImporterForSE_1 AssetGraphOnPreprocessAudio started.");
		
		Debug.Log("SampleImporterForSE_1 AssetGraphOnPreprocessAudio completed.");
	}
	
}