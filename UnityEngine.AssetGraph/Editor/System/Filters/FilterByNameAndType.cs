using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomFilter("Filter by Filename and Type")]
	public class FilterByNameAndType : IFilter {

		[SerializeField] private string m_filterKeyword;
		[SerializeField] private string m_filterKeytype;

		public string Label { 
			get {
				if(m_filterKeytype == Model.Settings.DEFAULT_FILTER_KEYTYPE) {
					return m_filterKeyword;
				} else {
					var pointIndex = m_filterKeytype.LastIndexOf('.');
					var keytypeName = (pointIndex > 0)? m_filterKeytype.Substring(pointIndex+1):m_filterKeytype;
					return string.Format("{0}[{1}]", m_filterKeyword, keytypeName);
				}
			}
		}

		public FilterByNameAndType() {
			m_filterKeyword = Model.Settings.DEFAULT_FILTER_KEYWORD;
			m_filterKeytype = Model.Settings.DEFAULT_FILTER_KEYTYPE;
		}

		public FilterByNameAndType(string name, string type) {
			m_filterKeyword = name;
			m_filterKeytype = type;
		}

		public bool FilterAsset(AssetReference a) {
			bool keywordMatch = Regex.IsMatch(a.importFrom, m_filterKeyword, 
				RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	
			bool match = keywordMatch;
	
			if(keywordMatch && m_filterKeytype != Model.Settings.DEFAULT_FILTER_KEYTYPE) 
			{
				var assumedType = a.filterType;
				match = assumedType != null && m_filterKeytype == assumedType.ToString();
			}
	
			return match;
		}

		public void OnInspectorGUI (Action onValueChanged) {

			var keyword = m_filterKeyword;

			GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

			using (new EditorGUILayout.HorizontalScope()) {
				keyword = EditorGUILayout.TextField(m_filterKeyword, s, GUILayout.Width(120));
				if (GUILayout.Button(m_filterKeytype , "Popup")) {
					NodeGUI.ShowFilterKeyTypeMenu(
						m_filterKeytype,
						(string selectedTypeStr) => {
							m_filterKeytype = selectedTypeStr;
							onValueChanged();
						} 
					);
				}
				if (keyword != m_filterKeyword) {
					m_filterKeyword = keyword;
					onValueChanged();
				}
			}
		}
	}
}