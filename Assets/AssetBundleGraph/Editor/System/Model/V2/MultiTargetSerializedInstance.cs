using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

using AssetBundleGraph;

namespace AssetBundleGraph.V2 {

	[Serializable] 
	public class MultiTargetSerializedInstance<T> where T: IJSONSerializable {

		[Serializable]
		public class Entry {
			[SerializeField] public BuildTargetGroup targetGroup;
			[SerializeField] public SerializedInstance<T> value;

			public Entry(BuildTargetGroup g, SerializedInstance<T> v) {
				targetGroup = g;
				value = v;
			}
		}

		[SerializeField] private List<Entry> m_values;

		public MultiTargetSerializedInstance(SerializedInstance<T> value) {
			m_values = new List<Entry>();
			this[BuildTargetUtility.DefaultTarget] = value;
		}

		public MultiTargetSerializedInstance() {
			m_values = new List<Entry>();
		}

		public List<Entry> Values {
			get {
				return m_values;
			}
		}

		public SerializedInstance<T> this[BuildTargetGroup g] {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					return DefaultValue;
				}
			}
			set {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					m_values[i].value = value;
				} else {
					m_values.Add(new Entry(g, value));
				}
			}
		}

		public SerializedInstance<T> this[BuildTarget index] {
			get {
				return this[BuildTargetUtility.TargetToGroup(index)];
			}
			set {
				this[BuildTargetUtility.TargetToGroup(index)] = value;
			}
		}

		public SerializedInstance<T> DefaultValue {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == BuildTargetUtility.DefaultTarget);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					var defaultValue = new SerializedInstance<T>();
					m_values.Add(new Entry(BuildTargetUtility.DefaultTarget, defaultValue));
					return defaultValue;
				}
			}
			set {
				this[BuildTargetUtility.DefaultTarget] = value;
			}
		}

		public SerializedInstance<T> CurrentPlatformValue {
			get {
				return this[EditorUserBuildSettings.selectedBuildTargetGroup];
			}
		}

		public bool ContainsValueOf (BuildTargetGroup group) {
			return m_values.FindIndex(v => v.targetGroup == group) >= 0;
		}

		public void Remove (BuildTargetGroup group) {
			int index = m_values.FindIndex(v => v.targetGroup == group);
			if(index >= 0) {
				m_values.RemoveAt(index);
			}
		}

		public override bool Equals(object rhs)
		{
			MultiTargetSerializedInstance<T> other = rhs as MultiTargetSerializedInstance<T>; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return this.m_values.GetHashCode(); 
		}

		public static bool operator == (MultiTargetSerializedInstance<T> lhs, MultiTargetSerializedInstance<T> rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

			if( lhs.m_values.Count != rhs.m_values.Count ) {
				return false;
			}

			foreach(var l in lhs.m_values) {
				if(rhs[l.targetGroup] != l.value) {
					return false;
				}
			}

			return true;
		}

		public static bool operator != (MultiTargetSerializedInstance<T> lhs, MultiTargetSerializedInstance<T> rhs) {
			return !(lhs == rhs);
		}
	}
}

