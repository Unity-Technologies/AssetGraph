using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public abstract class Node {

		#region Node input output types

		/**
		 * NodeInputType returns valid type of input for this node.
		 */ 
		public virtual Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

		/**
		 * NodeOutputType returns output data type from this node.
		 */ 
		public virtual Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}
		#endregion

		/**
		 * Category returns label string displayed at bottom of node
		 */ 
		public abstract string Category {
			get;
		}


		#region Initialization, Copy, Comparison, Validation
		/**
		 * Initialize Node with given NodeData.
		 */ 
		public abstract void Initialize(Model.NodeData data);

		/**
		 * Create duplicated copy of this Node.
		 */ 
		public abstract Node Clone(Model.NodeData newData);

		/**
		 * Test if input point is valid on this Node.
		 */ 
		public virtual bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
		}
		#endregion

		#region Build

		/**
		 *	Prepare is the method which validates and perform necessary setups in order to build.
		 * @param [in] 	target				target platform
		 * @param [in]	nodeData			NodeData instance for this node.
		 * @param [in]	incoming			incoming group of assets for this node on executing graph.
		 * @param [in]	connectionsToOutput	outgoing connections from this node.
		 * @param [in]	outputFunc			an interface to set outgoing group of assets.
		 */
		public virtual void Prepare (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc) 
		{
			// Do nothing
		}

		/**
		 * Build is the method which actualy performs the build. It is always called after Setup() is performed.
		 * @param [in] 	target				target platform
		 * @param [in]	nodeData			NodeData instance for this node.
		 * @param [in]	incoming			incoming group of assets for this node on executing graph.
		 * @param [in]	connectionsToOutput	outgoing connections from this node.
		 * @param [in]	outputFunc			an interface to set outgoing group of assets.
		 * @param [in]	progressFunc		an interface to display progress.
		 */
		public virtual void Build (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			Action<Model.NodeData, string, float> progressFunc)
		{
			// Do nothing
		}

		#endregion

		#region GUI
		/*
		 * ActiveStyle/InactiveStyle returns GUIStyle
		 */ 
		public abstract string ActiveStyle 	 { get; }
		public abstract string InactiveStyle { get; }

		/**
		 * OnInspectorGUI() is called when drawing Inspector of this Node.
		 * @param [in]	node			NodeGUI instance for this node.
		 * @param [in]	streamManager	Manager instance to retrieve graph's incoming/outgoing group of assets.
		 * @param [in]	editor			helper instance to draw inspector.
		 * @param [in]	onValueChanged	Action to call when OnInspectorGUI() changed value of this node.
		 */ 
		public abstract void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged);

		/*
		 * OnContextMenuGUI() is called when Node is clicked for context menu.
		 * @param [in]	menu	Context menu instance.
		 */
		public virtual void OnContextMenuGUI(GenericMenu menu) {
			// Do nothing
		}

		/**
		 * OnAssetsReimported() is called when there are changes of assets during editing graph.
		 * @param [in]	node				NodeGUI instance for this node.
		 * @param [in]	streamManager		Manager instance to retrieve graph's incoming/outgoing group of assets.
		 * @param [in] 	target				target platform
		 * @param [in]	importedAssets		Imported asset paths.
		 * @param [in]	deletedAssets		Deleted asset paths.
		 * @param [in]	movedAssets			Moved asset paths.
		 * @param [in]	movedFromAssetPaths	Original paths of moved assets.
		 */ 
		public virtual bool OnAssetsReimported(
			Model.NodeData nodeData,
			AssetReferenceStreamManager streamManager,
			BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
			return false;
		}

		#endregion
	}

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomNode : Attribute {

		private string m_name;
		private int m_orderPriority;

		public static readonly int kDEFAULT_PRIORITY = 1000;

		public string Name {
			get {
				return m_name;
			}
		}

		public int OrderPriority {
			get {
				return m_orderPriority;
			}
		}

		public CustomNode (string name) {
			m_name = name;
			m_orderPriority = kDEFAULT_PRIORITY;
		}

		public CustomNode (string name, int orderPriority) {
			m_name = name;
			m_orderPriority = orderPriority;
		}
	}

	public struct CustomNodeInfo : IComparable {
		public CustomNode node;
		public Type type;

		public CustomNodeInfo(Type t, CustomNode n) {
			node = n;
			type = t;
		}

		public Node CreateInstance() {
            string typeName = type.AssemblyQualifiedName;
            object o = type.Assembly.CreateInstance(type.FullName);
			return (Node) o;
		}

		public int CompareTo(object obj) {
			if (obj == null) {
				return 1;
			}

			CustomNodeInfo rhs = (CustomNodeInfo)obj;
			return node.OrderPriority - rhs.node.OrderPriority;
		}
	}

	public class NodeUtility {

		private static List<CustomNodeInfo> s_customNodes;

		public static List<CustomNodeInfo> CustomNodeTypes {
			get {
				if(s_customNodes == null) {
					s_customNodes = BuildCustomNodeList();
				}
				return s_customNodes;
			}
		}

		private static List<CustomNodeInfo> BuildCustomNodeList() {
			var list = new List<CustomNodeInfo>();

            var allNodes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var nodes = assembly.GetTypes()
                    .Where(t => t != typeof(Node))
                    .Where(t => typeof(Node).IsAssignableFrom(t));
                allNodes.AddRange (nodes);
            }

            foreach (var type in allNodes) {
				CustomNode attr = 
					type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;

				if (attr != null) {
					list.Add(new CustomNodeInfo(type, attr));
				}
			}

			list.Sort();

			return list;
		}

		public static bool HasValidCustomNodeAttribute(Type t) {
			CustomNode attr = 
				t.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetNodeGUIName(Node node) {
			CustomNode attr = 
				node.GetType().GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
			if(attr != null) {
				return attr.Name;
			}
			return string.Empty;
		}

		public static string GetNodeGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomNode attr = 
                    type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static int GetNodeOrderPriority(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomNode attr = 
                    type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
				if(attr != null) {
					return attr.OrderPriority;
				}
			}
			return CustomNode.kDEFAULT_PRIORITY;
		}

		public static Node CreateNodeInstance(string assemblyQualifiedName) {
			if(assemblyQualifiedName != null) {
                var type = Type.GetType(assemblyQualifiedName);

                return (Node) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}
	}
}
