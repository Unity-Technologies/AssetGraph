using System;
using UnityEngine;
using UnityEditor;

using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {

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

		/**
		 * Serialize this Modifier to JSON using JsonUtility.
		 */ 
		string Serialize();
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
		private static Dictionary<Type, Dictionary<string, string>> s_attributeClassNameMap;

		public static Dictionary<string, string> GetAttributeClassNameMap (Type targetType) {

			UnityEngine.Assertions.Assert.IsNotNull(targetType);

			if(s_attributeClassNameMap == null) {
				s_attributeClassNameMap =  new Dictionary<Type, Dictionary<string, string>>();
			}

			if(!s_attributeClassNameMap.Keys.Contains(targetType)) {
				var map = new Dictionary<string, string>(); 

				var builders = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.Where(t => t != typeof(IModifier))
					.Where(t => typeof(IModifier).IsAssignableFrom(t));

				foreach (var type in builders) {
					CustomModifier attr = 
						type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;

					if (attr != null && attr.For == targetType) {
						if (!map.ContainsKey(attr.Name)) {
							map[attr.Name] = type.FullName;
						} else {
							LogUtility.Logger.LogWarning(LogUtility.kTag, "Multiple CustomModifier class with the same name/type found. Ignoring " + type.Name);
						}
					}
				}
				s_attributeClassNameMap[targetType] = map;
			}
			return s_attributeClassNameMap[targetType];
		}

		public static string GetModifierGUIName(IModifier m) {
			CustomModifier attr = 
				m.GetType().GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
			return attr.Name;
		}

		public static string GetModifierGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomModifier attr = 
					Type.GetType(className).GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static string GUINameToClassName(string guiName, Type targetType) {
			var map = GetAttributeClassNameMap(targetType);

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
				CustomModifier attr = 
					Type.GetType(className).GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
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

		public static IModifier CreateModifier(NodeData node, BuildTarget target) {
			return CreateModifier(node, BuildTargetUtility.TargetToGroup(target));
		}

		public static IModifier CreateModifier(NodeData node, BuildTargetGroup targetGroup) {
			return InstanceDataUtility<IModifier>.CreateInstance(node, targetGroup);
		}

		public static IModifier CreateModifier(string guiName, Type targetType) {
			var className = GUINameToClassName(guiName, targetType);
			if(className != null) {
				return (IModifier) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}

		public static IModifier CreateModifier(string className) {

			if(className == null) {
				return null;
			}

			Type t = Type.GetType(className);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomModifierAttribute(t)) {
				return null;
			}

			return (IModifier) Assembly.GetExecutingAssembly().CreateInstance(className);
		}
	}
}