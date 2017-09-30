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

    /// <summary>
    /// IModifier is an interface which modifies incoming assets.
    /// Subclass of IModifier must have <c>CustomModifier</c> attribute.
    /// </summary>
	public interface IModifier {
        /// <summary>
        /// Test if incoming assset is different from this IModifier's setting.
        /// </summary>
        /// <returns><c>true</c> if this instance is modified the specified assets; otherwise, <c>false</c>.</returns>
        /// <param name="assets">Assets.</param>
		bool IsModified (UnityEngine.Object[] assets);

        /// <summary>
        /// Modify incoming assets.
        /// </summary>
        /// <param name="assets">Assets.</param>
		void Modify (UnityEngine.Object[] assets);

        /// <summary>
        /// Draw Inspector GUI for this Modifier.
        /// </summary>
        /// <param name="onValueChanged">On value changed.</param>
		void OnInspectorGUI (Action onValueChanged);
	}

    /// <summary>
    /// CustomModifier attribute is to declare the class is used as a IModifier. 
    /// </summary>
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomModifier : Attribute {
		private string m_name;
		private Type m_modifyFor;

        /// <summary>
        /// Name of Modifier appears on GUI.
        /// </summary>
        /// <value>The name.</value>
		public string Name {
			get {
				return m_name;
			}
		}

        /// <summary>
        /// Type of asset Modifier modifies.
        /// </summary>
        /// <value>For.</value>
		public Type For {
			get {
				return m_modifyFor;
			}
		}

        /// <summary>
        /// CustomModifier declares the class is used as a IModifier.
        /// </summary>
        /// <param name="name">Name of Modifier appears on GUI.</param>
        /// <param name="modifyFor">Type of asset Modifier modifies.</param>
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