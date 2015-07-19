using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForModel : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessModel () {
		Debug.Log("SampleImporterForModel AssetGraphOnPreprocessModel started.");
		
		Debug.Log("SampleImporterForModel AssetGraphOnPreprocessModel completed.");
	}
	
}