using System;
using UnityEngine;

using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomModifier : Attribute {
		private string m_name;
		private Type m_modifyFor;

		public string Name {
			get {
				return m_name;
			}
		}

		public Type For {
			get {
				return m_modifyFor;
			}
		}

		public CustomModifier (string name, Type modifyFor) {
			m_name = name;
			m_modifyFor = modifyFor;
		}
	}

	[Serializable] 
	public class Modifier {

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
					.Where(t => t != typeof(Modifier))
					.Where(t => typeof(Modifier).IsAssignableFrom(t));

				foreach (var type in builders) {
					CustomModifier attr = 
						type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;

					if (attr != null && attr.For == targetType) {
						if (!map.ContainsKey(attr.Name)) {
							map[attr.Name] = type.FullName;
						} else {
							Debug.LogWarning("Multiple CustomModifier class with the same name/type found. Ignoring " + type.Name);
						}
					}
				}
				s_attributeClassNameMap[targetType] = map;
			}
			return s_attributeClassNameMap[targetType];
		}


		[SerializeField] public string operatorType;

		public Modifier () {}// this class is required for serialization. and reflection

        public virtual Modifier DefaultSetting () {
			throw new Exception("Not Implemented. Subclass must override DefaultSetting method.");
		}

		public virtual bool IsChanged<T> (T asset) {
			throw new Exception("Not Implemented. Subclass must override IsChanged method.");
		}

		public virtual void Modify<T> (T asset) {
			throw new Exception("Not Implemented. Subclass must override Modify method.");
		}

        public virtual void DrawInspector (Action changed) {
			throw new Exception("Not Implemented. Subclass must override DrawInspector method.");
        }
    }
}