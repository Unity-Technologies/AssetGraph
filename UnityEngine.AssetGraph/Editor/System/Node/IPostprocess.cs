using UnityEngine;

using System;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
    /// <summary>
    /// Postprocess interface for ABGT.
    /// </summary>
	public interface IPostprocess {
        /// <summary>
        /// Dos the postprocess.
        /// </summary>
        /// <param name="buildReports">Build reports.</param>
        /// <param name="exportReports">Export reports.</param>
		void DoPostprocess (IEnumerable<AssetBundleBuildReport> buildReports, IEnumerable<ExportReport> exportReports);
	}
}
