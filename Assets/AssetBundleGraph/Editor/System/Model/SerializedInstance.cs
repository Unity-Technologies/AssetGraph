using UnityEngine;
using System;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class SerializedInstance<T> where T: IJSONSerializable {

		[SerializeField] private string m_className;
		[SerializeField] private string m_instanceData;

		private T m_object;

		public string ClassName {
			get {
				return m_className;
			}
		}

		public T Object {
			get {
				if(m_object == null) {
					m_object = Deserialize();
				}
				return m_object;
			}
		}

		public SerializedInstance() {
			m_className = null;
			m_instanceData = null;
		}

		public SerializedInstance(T obj) {
			UnityEngine.Assertions.Assert.IsNotNull((IJSONSerializable)obj);

			m_className = obj.GetType().AssemblyQualifiedName;
			m_instanceData = CustomScriptUtility.EncodeString(obj.Serialize());
		}

		private T Deserialize() {
			Type instanceType = null;
			if(!string.IsNullOrEmpty(m_className)) {
				instanceType = Type.GetType(m_className);
			}

			if(m_instanceData != null && instanceType != null) {
				string data = CustomScriptUtility.DecodeString(m_instanceData);
				return (T)JsonUtility.FromJson(data, instanceType);
			}

			return default(T);
		}

		public void Save() {
			if(m_object != null) {
				UnityEngine.Assertions.Assert.AreEqual(m_className, m_object.GetType().AssemblyQualifiedName);
				CustomScriptUtility.EncodeString(m_object.Serialize());
			}
		}

		public T Clone() {
			return Deserialize();
		}

		public override bool Equals(object rhs)
		{
			SerializedInstance<T> other = rhs as SerializedInstance<T>; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return (m_instanceData == null)? this.GetHashCode() : m_instanceData.GetHashCode();
		}

		public static bool operator == (SerializedInstance<T> lhs, SerializedInstance<T> rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

			return lhs.m_className != rhs.m_className && lhs.m_instanceData == rhs.m_instanceData;
		}

		public static bool operator != (SerializedInstance<T> lhs, SerializedInstance<T> rhs) {
			return !(lhs == rhs);
		}
	}
}

