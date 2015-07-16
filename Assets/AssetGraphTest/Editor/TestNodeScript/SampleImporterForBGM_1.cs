using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForBGM_1 : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessAudio () {
		Debug.Log("SampleImporterForBGM_1 AssetGraphOnPreprocessAudio started.");
		
		Debug.Log("SampleImporterForBGM_1 AssetGraphOnPreprocessAudio completed.");
	}
	
}