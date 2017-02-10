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

		public virtual Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}

		public virtual Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.Assets;
			}
		}
		#endregion

		#region Initialization, Copy, Comparison, Validation
		public abstract void Initialize(Model.NodeData data);
		public abstract bool IsEqual(Node node);
		public abstract Node Clone();

		public virtual bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
		}
		#endregion

		#region Build

		/**
		 *	Prepare is the method which validates and perform necessary setups in order to build.
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
		public abstract string ActiveStyle 	 { get; }
		public abstract string InactiveStyle { get; }

		/**
		 * Provide Editing interface on Inspector Window.
		 */ 
		public abstract void OnInspectorGUI(NodeGUI node, NodeGUIEditor editor, Action onValueChanged);

		public virtual void OnContextMenuGUI(GenericMenu menu) {
			// Do nothing
		}

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
			string typeName = type.FullName;

			object o = Assembly.GetExecutingAssembly().CreateInstance(typeName);
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

			var nodes = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t != typeof(Node))
				.Where(t => typeof(Node).IsAssignableFrom(t));

			foreach (var type in nodes) {
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
					Type.GetType(className).GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
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
					Type.GetType(className).GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;
				if(attr != null) {
					return attr.OrderPriority;
				}
			}
			return CustomNode.kDEFAULT_PRIORITY;
		}

		public static Node CreateNodeInstance(string className) {
			if(className != null) {
				return (Node) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}
	}
}
