using UnityEngine;

using System;
using System.Collections.Generic;

// Test on compile

public partial class Test {
	public void RunTests () {
		var tests = new List<Action>();
		
		{
			tests.Add(this._0_0_SetupFilter);
			tests.Add(this._0_1_RunFilter);
			tests.Add(this._0_2_SetupImporter);
			tests.Add(this._0_3_RunImporter);
			tests.Add(this._0_4_SetupPrefabricator);
			tests.Add(this._0_5_RunPrefabricator);
			tests.Add(this._0_6_SetupBundlizer);
			tests.Add(this._0_7_RunBundlizer);
			tests.Add(this._0_8_SerializeGraph);
			tests.Add(this._0_9_RunStackedGraph);
		}


			
		Debug.LogError("test start date:" + DateTime.Now);
		foreach (var test in tests) {
			Setup();
			test();
			Teardown();
		}
	}


	public void Setup () {
		
	}

	public void Teardown () {
		
	}
}