using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

public class SampleImporterForModel_1 : AssetGraph.ImporterBase {

	public override void AssetGraphOnPreprocessModel () {
		Debug.Log("SampleImporterForModel_1 AssetGraphOnPreprocessModel started.");
		
		Debug.Log("SampleImporterForModel_1 AssetGraphOnPreprocessModel completed.");
	}
	
}