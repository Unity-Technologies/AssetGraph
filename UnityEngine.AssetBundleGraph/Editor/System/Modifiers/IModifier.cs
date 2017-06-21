using System;
using UnityEngine;
using UnityEditor;

using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	/**
	 * IModifier is an interface which modifies incoming assets.
	 * Subclass of IModifier must have CustomModifier attribute.
	 */
	public interface IModifier {
		/**
		 * Test if incoming assset is different from this IModifier's setting.
		 * asset is always type of object defined
		 */ 
		bool IsModified (UnityEngine.Object[] assets);

		/**
		 * Modifies incoming asset.
		 */ 
		void Modify (UnityEngine.Object[] assets);

		/**
		 * Draw Inspector GUI for this Modifier.
		 */ 
		void OnInspectorGUI (Action onValueChanged);
	}

	/**
	 * Used to declare the class is used as a IModifier. 
	 * Classes with CustomModifier attribute must implement IModifier interface.
	 */ 
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomModifier : Attribute {
		private string m_name;
		private Type m_modifyFor;

		/**
		 * Name of Modifier appears on GUI.
		 */ 
		public string Name {
			get {
				return m_name;
			}
		}

		/**
		 * Type of asset Modifier modifies.
		 */ 
		public Type For {
			get {
				return m_modifyFor;
			}
		}

		/**
		 * CustomModifier declares the class is used as a IModifier.
		 * @param [in] name 	 Name of Modifier appears on GUI.
		 * @param [in] modifyFor Type of asset Modifier modifies.
		 */ 
		public CustomModifier (string name, Type modifyFor) {
			m_name = name;
			m_modifyFor = modifyFor;
		}
	}

	public class ModifierUtility {
        private static Dictionary<Type, Dictionary<string, string>> s_attributeAssemblyQualifiedNameMap;

		public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap (Type targetType) {

			UnityEngine.Assertions.Assert.IsNotNull(targetType);

			if(s_attributeAssemblyQualifiedNameMap == null) {
				s_attributeAssemblyQualifiedNameMap =  new Dictionary<Type, Dictionary<string, string>>();
			}

			if(!s_attributeAssemblyQualifiedNameMap.Keys.Contains(targetType)) {
				var map = new Dictionary<string, string>(); 

                var allBuilders = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var builders = assembly.GetTypes()
                        .Where(t => t != typeof(IModifier))
                        .Where(t => typeof(IModifier).IsAssignableFrom(t));
                    allBuilders.AddRange (builders);
                }

                foreach (var type in allBuilders) {
					CustomModifier attr = 
						type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;

					if (attr != null && attr.For == targetType) {
						if (!map.ContainsKey(attr.Name)) {
							map[attr.Name] = type.AssemblyQualifiedName;
						} else {
							LogUtility.Logger.LogWarning(LogUtility.kTag, "Multiple CustomModifier class with the same name/type found. Ignoring " + type.Name);
						}
					}
				}
				s_attributeAssemblyQualifiedNameMap[targetType] = map;
			}
			return s_attributeAssemblyQualifiedNameMap[targetType];
		}

		public static string GetModifierGUIName(IModifier m) {
			CustomModifier attr = 
				m.GetType().GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
			return attr.Name;
		}

		public static string GetModifierGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomModifier attr = type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static string GUINameToAssemblyQualifiedName(string guiName, Type targetType) {
			var map = GetAttributeAssemblyQualifiedNameMap(targetType);

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static Type GetModifierTargetType(IModifier m) {
			CustomModifier attr = 
				m.GetType().GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
			UnityEngine.Assertions.Assert.IsNotNull(attr);
			return attr.For;
		}

		public static Type GetModifierTargetType(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomModifier attr = type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
				if(attr != null) {
					return attr.For;
				}
			}
			return null;
		}

		public static bool HasValidCustomModifierAttribute(Type t) {
			CustomModifier attr = 
				t.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;

			if(attr != null) {
				return !string.IsNullOrEmpty(attr.Name) && attr.For != null;
			}
			return false;
		}

		public static IModifier CreateModifier(string guiName, Type targetType) {
            var assemblyQualifiedName = GUINameToAssemblyQualifiedName(guiName, targetType);
			if(assemblyQualifiedName != null) {
                var type = Type.GetType(assemblyQualifiedName);
                if (type == null) {
                    return null;
                }

                return (IModifier) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}

		public static IModifier CreateModifier(string assemblyQualifiedName) {

			if(assemblyQualifiedName == null) {
				return null;
			}

			Type t = Type.GetType(assemblyQualifiedName);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomModifierAttribute(t)) {
				return null;
			}

            return (IModifier) t.Assembly.CreateInstance(t.FullName);
		}
	}
}