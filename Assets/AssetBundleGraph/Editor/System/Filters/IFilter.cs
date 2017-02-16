using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	/**
	 * IFilter is an interface to create custom filter condition.
	 * Subclass of IFilter must have CUstomFilter attribute.
	 */
	public interface IFilter {

		string Label { get; }

		bool FilterAsset(AssetReference asset);

		/**
		 * Draw Inspector GUI for this Filter.
		 */ 
		void OnInspectorGUI (Action onValueChanged);
	}

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomFilter : Attribute {
		private string m_name;

		public string Name {
			get {
				return m_name;
			}
		}

		public CustomFilter (string name) {
			m_name = name;
		}
	}

	public class FilterUtility {

		private static  Dictionary<string, string> s_attributeClassNameMap;

		public static Dictionary<string, string> GetAttributeClassNameMap () {

			if(s_attributeClassNameMap == null) {
				// attribute name or class name : class name
				s_attributeClassNameMap = new Dictionary<string, string>(); 

				var builders = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.Where(t => !t.IsInterface)
					.Where(t => typeof(IFilter).IsAssignableFrom(t));

				foreach (var type in builders) {
					// set attribute-name as key of dict if atribute is exist.
					CustomFilter attr = 
						type.GetCustomAttributes(typeof(CustomFilter), true).FirstOrDefault() as CustomFilter;

					var typename = type.ToString();


					if (attr != null) {
						if (!s_attributeClassNameMap.ContainsKey(attr.Name)) {
							s_attributeClassNameMap[attr.Name] = typename;
						}
					} else {
						s_attributeClassNameMap[typename] = typename;
					}
				}
			}
			return s_attributeClassNameMap;
		}

		public static string GetFilterGUIName(IFilter filter) {
			CustomFilter attr = 
				filter.GetType().GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
			return attr.Name;
		}

		public static string GetPrefabBuilderGUIName(string className) {
			if(className != null) {
				var type = Type.GetType(className);
				if(type != null) {
					CustomFilter attr = 
						Type.GetType(className).GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
					if(attr != null) {
						return attr.Name;
					}
				}
			}
			return string.Empty;
		}

		public static string GUINameToClassName(string guiName) {
			var map = GetAttributeClassNameMap();

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static IFilter CreateFilter(string guiName) {
			var className = GUINameToClassName(guiName);
			if(className != null) {
				return (IFilter) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}

		public static bool HasValidCustomFilterAttribute(Type t) {
			CustomFilter attr = 
				t.GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static IFilter CreateFilterByClassName(string className) {

			if(className == null) {
				return null;
			}

			Type t = Type.GetType(className);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomFilterAttribute(t)) {
				return null;
			}

			return (IFilter) Assembly.GetExecutingAssembly().CreateInstance(className);
		}
	}
}