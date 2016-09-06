using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {

	[Serializable] 
	public class SerializableMultiTargetVector2 {

		[Serializable]
		public class Entry {
			[SerializeField] public BuildTargetGroup targetGroup;
			[SerializeField] public Vector2 value;

			public Entry(BuildTargetGroup g, Vector2 v) {
				targetGroup = g;
				value = v;
			}
		}

		[SerializeField] private List<Entry> m_values;

		public SerializableMultiTargetVector2(Vector2 value) {
			m_values = new List<Entry>();
			this[AssetBundleGraphPlatformSettings.DefaultTarget] = value;
		}

		public SerializableMultiTargetVector2() {
			m_values = new List<Entry>();
		}

		public SerializableMultiTargetVector2(MultiTargetProperty<Vector2> property) {
			m_values = new List<Entry>();
			foreach(var k in property.Keys) {
				m_values.Add(new Entry(k, property[k]));
			}
		}

		public List<Entry> Values {
			get {
				return m_values;
			}
		}

		public Vector2 this[BuildTargetGroup g] {
			get {
				int i = m_values.FindIndex(v => v.targetGroup == g);
				if(i >= 0) {
					return m_values[i].value;
				} else {
					return Vector2.zero;
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

		public Vector2 this[BuildTarget index] {
			get {
				return this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)];
			}
			set {
				this[AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(index)] = value;
			}
		}

		public Vector2 DefaultValue {
			get {
				return this[AssetBundleGraphPlatformSettings.DefaultTarget];
			}
			set {
				this[AssetBundleGraphPlatformSettings.DefaultTarget] = value;
			}
		}

		public Vector2 CurrentPlatformValue {
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

		public MultiTargetProperty<Vector2> ToProperty () {
			MultiTargetProperty<Vector2> p = new MultiTargetProperty<Vector2>();

			foreach(Entry e in m_values) {
				p.Set(e.targetGroup, e.value);
			}

			return p;
		}
	}
}
