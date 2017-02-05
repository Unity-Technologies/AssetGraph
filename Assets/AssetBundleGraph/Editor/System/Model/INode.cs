using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public interface INode : IJSONSerializable {

		string ActiveStyle {
			get;
		}

		string InactiveStyle {
			get;
		}

		Model.NodeOutputSemantics NodeInputType {
			get;
		}

		Model.NodeOutputSemantics NodeOutputType {
			get;
		}

		/**
			Prepare is the method which validates and perform necessary setups in order to build.
		*/
		void Prepare (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc);

		/**
			Build is the method which actualy performs the build. It is always called after Setup() is performed.
		*/
		void Build (BuildTarget target, 
			Model.NodeData nodeData, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			Action<Model.NodeData, string, float> progressFunc);

		void OnInspectorGUI(NodeGUI node, NodeGUIEditor editor);
		void OnNodeGUI(NodeGUI node);

		void Initialize(Model.NodeData data);

		INode Clone();

		bool IsEqual(INode node);

		bool IsValidInputConnectionPoint(Model.ConnectionPointData point);

		bool OnAssetsReimported(BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths);
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

		public INode CreateInstance() {
			return (INode) type.Assembly.CreateInstance(type.AssemblyQualifiedName);
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
				.Where(t => t != typeof(INode))
				.Where(t => typeof(INode).IsAssignableFrom(t));

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

		public static string GetNodeGUIName(INode node) {
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

		public static INode CreateNodeInstance(string className) {
			if(className != null) {
				return (INode) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}
	}
}
