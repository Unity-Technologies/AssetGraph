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
	 * IPrefabBuilder is an interface to create Prefab AssetReference from incoming asset group.
	 * Subclass of IPrefabBuilder must have CUstomPrefabBuilder attribute.
	 */
	public interface IPrefabBuilder {
		/**
		 * Test if prefab can be created with incoming assets.
		 * @result Name of prefab file if prefab can be created. null if not.
		 */
		string CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects);

		/**
		 * Create Prefab.
		 */ 
		UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects);

		/**
		 * Draw Inspector GUI for this PrefabBuilder.
		 */ 
		void OnInspectorGUI (Action onValueChanged);
	}

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomPrefabBuilder : Attribute {
		private string m_name;
		private string m_version;
		private int m_assetThreshold;

		private const int kDEFAULT_ASSET_THRES = 10;

		public string Name {
			get {
				return m_name;
			}
		}

		public string Version {
			get {
				return m_version;
			}
		}

		public int AssetThreshold {
			get {
				return m_assetThreshold;
			}
		}

		public CustomPrefabBuilder (string name) {
			m_name = name;
			m_version = string.Empty;
			m_assetThreshold = kDEFAULT_ASSET_THRES;
		}

		public CustomPrefabBuilder (string name, string version) {
			m_name = name;
			m_version = version;
			m_assetThreshold = kDEFAULT_ASSET_THRES;
		}

		public CustomPrefabBuilder (string name, string version, int itemThreashold) {
			m_name = name;
			m_version = version;
			m_assetThreshold = itemThreashold;
		}
	}

	public class PrefabBuilderUtility {

        private static  Dictionary<string, string> s_attributeAssemblyQualifiedNameMap;

		public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap () {

			if(s_attributeAssemblyQualifiedNameMap == null) {
				// attribute name or class name : class name
				s_attributeAssemblyQualifiedNameMap = new Dictionary<string, string>(); 

                var allBuilders = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var builders = assembly.GetTypes()
                        .Where(t => !t.IsInterface)
                        .Where(t => typeof(IPrefabBuilder).IsAssignableFrom(t));
                    allBuilders.AddRange (builders);
                }

                foreach (var type in allBuilders) {
					// set attribute-name as key of dict if atribute is exist.
					CustomPrefabBuilder attr = 
						type.GetCustomAttributes(typeof(CustomPrefabBuilder), true).FirstOrDefault() as CustomPrefabBuilder;

                    var typename = type.AssemblyQualifiedName;


					if (attr != null) {
						if (!s_attributeAssemblyQualifiedNameMap.ContainsKey(attr.Name)) {
							s_attributeAssemblyQualifiedNameMap[attr.Name] = typename;
						}
					} else {
						s_attributeAssemblyQualifiedNameMap[typename] = typename;
					}
				}
			}
			return s_attributeAssemblyQualifiedNameMap;
		}

		public static string GetPrefabBuilderGUIName(IPrefabBuilder builder) {
			CustomPrefabBuilder attr = 
				builder.GetType().GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr.Name;
		}

		public static bool HasValidCustomPrefabBuilderAttribute(Type t) {
			CustomPrefabBuilder attr = 
				t.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetPrefabBuilderGUIName(string className) {
			if(className != null) {
				var type = Type.GetType(className);
				if(type != null) {
					CustomPrefabBuilder attr = 
                        type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
					if(attr != null) {
						return attr.Name;
					}
				}
			}
			return string.Empty;
		}

		public static string GetPrefabBuilderVersion(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomPrefabBuilder attr = 
                    type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
				if(attr != null) {
					return attr.Version;
				}
			}
			return string.Empty;
		}

		public static int GetPrefabBuilderAssetThreshold(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomPrefabBuilder attr = 
                    type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
				if(attr != null) {
					return attr.AssetThreshold;
				}
			}
			return 0;
		}

		public static string GUINameToAssemblyQualifiedName(string guiName) {
			var map = GetAttributeAssemblyQualifiedNameMap();

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilder(string guiName) {
			var className = GUINameToAssemblyQualifiedName(guiName);
			if(className != null) {
                var type = Type.GetType(className);
                if (type == null) {
                    return null;
                }
                return (IPrefabBuilder) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilderByAssemblyQualifiedName(string assemblyQualifiedName) {

			if(assemblyQualifiedName == null) {
				return null;
			}

			Type t = Type.GetType(assemblyQualifiedName);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomPrefabBuilderAttribute(t)) {
				return null;
			}

            return (IPrefabBuilder) t.Assembly.CreateInstance(t.FullName);
		}
	}
}