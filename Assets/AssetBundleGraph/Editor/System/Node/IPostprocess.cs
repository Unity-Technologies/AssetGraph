using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetBundleGraph.V2 {
	public interface IPostprocess {
		void DoPostprocess (IEnumerable<AssetBundleBuildReport> buildReports, IEnumerable<ExportReport> exportReports);
	}
}
