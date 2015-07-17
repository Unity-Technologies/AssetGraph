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
			// tests.Add(this._0_10_SetupExport);
			// tests.Add(this._0_11_RunExport);
			tests.Add(this._0_12_RunStackedGraph_FullStacked);
			// tests.Add(this._0_13_SetupStackedGraph_FullStacked);
		}


		// GUI
		{
			// tests.Add(this._1_0_AddNode);
			// tests.Add(this._1_1_DeleteNode);
			// tests.Add(this._1_2_AddConnection);
			// tests.Add(this._1_3_DeleteConnection);
		}

		// prefabricator
		{
			// tests.Add(this._2_0_PrefabricatorFromOutside);
			// tests.Add(this._2_1_PrefabricatorFromOutsideWithMeta);
		}

		// only 1 time run
		{
			// tests.Add(this._3_0_OrderWithCache0);
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