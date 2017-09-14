using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
    /// <summary>
    /// Node build context.
    /// </summary>
    public class NodeBuildContext {

        /// <summary>
        /// The target.
        /// </summary>
        public BuildTarget target;

        /// <summary>
        /// The node data.
        /// </summary>
        public Model.NodeData nodeData;

        /// <summary>
        /// The incoming.
        /// </summary>
        public IEnumerable<PerformGraph.AssetGroups> incoming;

        /// <summary>
        /// The connections to output.
        /// </summary>
        public IEnumerable<Model.ConnectionData> connectionsToOutput;

        /// <summary>
        /// The output func.
        /// </summary>
        public PerformGraph.Output outputFunc;

        /// <summary>
        /// The progress func.
        /// </summary>
        public Action<Model.NodeData, string, float> progressFunc;
	}
}
