using UnityEngine;

using System;
using System.Collections.Generic;

// Test on compile

public partial class Test {
	public void RunTests () {
		var tests = new List<Action>();
		
		// general
		{
			// tests.Add(this._0_0_0_SetupLoader);
			// tests.Add(this._0_0_1_RunLoader);
			// tests.Add(this._0_0_SetupFilter);
			// tests.Add(this._0_1_RunFilter);
			// tests.Add(this._0_2_SetupImporter);
			// tests.Add(this._0_3_RunImporter);
			// tests.Add(this._0_4_SetupPrefabricator);
			// tests.Add(this._0_5_RunPrefabricator);
			// tests.Add(this._0_6_SetupBundlizer);
			// tests.Add(this._0_7_RunBundlizer);
			// tests.Add(this._0_8_0_SerializeGraph_hasValidEndpoint);
			// tests.Add(this._0_8_1_SerializeGraph_hasValidOrder);
			// tests.Add(this._0_9_RunStackedGraph);
			// tests.Add(this._0_10_SetupExporter);
			// tests.Add(this._0_11_RunExporter);
			// tests.Add(this._0_12_RunStackedGraph_FullStacked);
			// tests.Add(this._0_13_SetupStackedGraph_FullStacked);
			// tests.Add(this._0_14_SetupStackedGraph_Sample);
			// tests.Add(this._0_15_RunStackedGraph_Sample);
		}

		// only 1 time run, use cache.
		{
			// tests.Add(this._3_0_OrderWithCache0);
		}

		// cache test
		{
			tests.Add(this._4_0_CacheWithSetup);
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