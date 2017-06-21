using UnityEngine;
using System;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class SerializedInstance<T> where T: class {

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

		public string Data {
			get {
				return m_instanceData;
			}
		}

		public SerializedInstance() {
			m_className = string.Empty;
			m_instanceData = string.Empty;
		}

		public SerializedInstance(SerializedInstance<T> instance) {
			m_className = instance.m_className;
			m_instanceData = instance.m_instanceData;
		}

		public SerializedInstance(T obj) {
			UnityEngine.Assertions.Assert.IsNotNull(obj);

            m_className = obj.GetType().AssemblyQualifiedName;
			m_instanceData = CustomScriptUtility.EncodeString(JsonUtility.ToJson(obj));
		}

		private T Deserialize() {
			Type instanceType = null;
			if(!string.IsNullOrEmpty(m_className)) {
				instanceType = Type.GetType(m_className);
			}

			if(!string.IsNullOrEmpty(m_instanceData) && instanceType != null) {
				string data = CustomScriptUtility.DecodeString(m_instanceData);
				return (T)JsonUtility.FromJson(data, instanceType);
			}

			return default(T);
		}

		public void Save() {
			if(m_object != null) {
                m_className = m_object.GetType().AssemblyQualifiedName;
				m_instanceData = CustomScriptUtility.EncodeString(JsonUtility.ToJson(m_object));
			}
		}

		public T Clone() {
			Save();
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

