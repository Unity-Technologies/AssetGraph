using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public abstract class Node : IJSONSerializable {

		protected Model.NodeData m_node;

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

		public abstract string ActiveStyle 	 { get; }
		public abstract string InactiveStyle { get; }
		public abstract Node Clone();
		public abstract bool IsEqual(Node node);
		public abstract string Serialize();

		public virtual void Initialize(Model.NodeData data) {
			m_node = data;
		}

		public virtual bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
		}

		/**
			Prepare is the method which validates and perform necessary setups in order to build.
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
			Build is the method which actualy performs the build. It is always called after Setup() is performed.
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

		public virtual void OnInspectorGUI(NodeGUI node, NodeGUIEditor editor) {
			// Do nothing
		}
		public virtual void OnNodeGUI(NodeGUI node) {
			// Do nothing
		}

		public virtual bool OnAssetsReimported(
			AssetReferenceStreamManager streamManager,
			BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
			return false;
		}
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

	public struct CustomNodeInfo {
		public CustomNode node;
		public Type type;

		public CustomNodeInfo(Type t, CustomNode n) {
			node = n;
			type = t;
		}

		public Node CreateInstance() {
			return (Node) type.Assembly.CreateInstance(type.AssemblyQualifiedName);
		}
	}

	public class NodeUtility {

		private static SortedList<int, CustomNodeInfo> s_customNodes;

		public static IEnumerable<CustomNodeInfo> CustomNodeTypes {
			get {
				if(s_customNodes == null) {
					s_customNodes = BuildCustomNodeList();
				}
				return s_customNodes.Values.AsEnumerable();
			}
		}

		private static SortedList<int, CustomNodeInfo> BuildCustomNodeList() {
			var list = new SortedList<int, CustomNodeInfo>();

			var nodes = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t != typeof(Node))
				.Where(t => typeof(Node).IsAssignableFrom(t));

			foreach (var type in nodes) {
				CustomNode attr = 
					type.GetCustomAttributes(typeof(CustomNode), false).FirstOrDefault() as CustomNode;

				if (attr != null) {
					list.Add(attr.OrderPriority, new CustomNodeInfo(type, attr));
				}
			}

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
