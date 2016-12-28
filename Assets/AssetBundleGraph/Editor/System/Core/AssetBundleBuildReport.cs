using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

/**
	static executor for AssetBundleGraph's data.
*/
namespace AssetBundleGraph {
	public class AssetBundleBuildReport {

		private class AssetBundleBuildReportManager {

			private List<AssetBundleBuildReport> m_buildReports;
			private List<ExportReport> m_exportReports;

			public List<AssetBundleBuildReport> BuildReports {
				get {
					return m_buildReports;
				}
			}

			public List<ExportReport> ExportReports {
				get {
					return m_exportReports;
				}
			}

			public AssetBundleBuildReportManager() {
				m_buildReports = new List<AssetBundleBuildReport>();
				m_exportReports = new List<ExportReport>();
			}
		}

		private static AssetBundleBuildReportManager s_mgr;
		private static AssetBundleBuildReportManager Manager {
			get {
				if(s_mgr == null) {
					s_mgr = new AssetBundleBuildReportManager();
				}
				return s_mgr;
			}
		}

		static public void ClearReports() {
			Manager.BuildReports.Clear();
			Manager.ExportReports.Clear();
		}

		static public void AddBuildReport(AssetBundleBuildReport r) {
			Manager.BuildReports.Add(r);
		}
		static public void AddExportReport(ExportReport r) {
			Manager.ExportReports.Add(r);
		}

		static public IEnumerable<AssetBundleBuildReport> BuildReports {
			get {
				return Manager.BuildReports;
			}
		}

		static public IEnumerable<ExportReport> ExportReports {
			get {
				return Manager.ExportReports;
			}
		}

		private NodeData m_node;
		private AssetBundleManifest m_manifest;
		private AssetBundleBuild[] m_bundleBuild;
		private List<AssetReference> m_builtBundles;
		private Dictionary<string, List<AssetReference>> m_assetGroups;
		private Dictionary<string, List<string>> m_bundleNamesAndVariants;

		public NodeData Node {
			get {
				return m_node;
			}
		}

		public AssetBundleManifest Manifest {
			get {
				return m_manifest;
			}
		}

		public AssetBundleBuild[] BundleBuild {
			get {
				return m_bundleBuild;
			}
		}

		public List<AssetReference> BuiltBundleFiles {
			get {
				return m_builtBundles;
			}
		}

		public Dictionary<string, List<AssetReference>> AssetGroups {
			get {
				return m_assetGroups;
			}
		}

		public IEnumerable<string> BundleNames {
			get {
				return m_bundleNamesAndVariants.Keys;
			}
		}

		public List<string> GetVariantNames(string bundleName) {
			if(m_bundleNamesAndVariants.ContainsKey(bundleName)) {
				return m_bundleNamesAndVariants[bundleName];
			}
			return null;
		}

		public AssetBundleBuildReport(
			NodeData node,
			AssetBundleManifest m, 
			AssetBundleBuild[] bb, 
			List<AssetReference> builtBundles,
			Dictionary<string, List<AssetReference>> ag, 
			Dictionary<string, List<string>> names) {
			m_node = node;
			m_manifest = m;
			m_bundleBuild = bb;
			m_builtBundles = builtBundles;
			m_assetGroups = ag;
			m_bundleNamesAndVariants = names;
		}
	}

	public class ExportReport {

		public class Entry {
			public string source;
			public string destination;
			public Entry(string src, string dst) {
				source = src;
				destination = dst;
			}
		}

		public class ErrorEntry {
			public string source;
			public string destination;
			public string reason;
			public ErrorEntry(string src, string dst, string r) {
				source = src;
				destination = dst;
				reason = r;
			}
		}

		private NodeData m_nodeData;

		private List<Entry> m_exportedItems;
		private List<ErrorEntry> m_failedItems;

		public List<Entry> ExportedItems {
			get {
				return m_exportedItems;
			}
		}

		public List<ErrorEntry> Errors {
			get {
				return m_failedItems;
			}
		}

		public NodeData Node {
			get {
				return m_nodeData;
			}
		}

		public ExportReport(NodeData node) {
			m_nodeData = node;

			m_exportedItems = new List<Entry>();
			m_failedItems = new List<ErrorEntry> ();
		}

		public void AddExportedEntry(string src, string dst) {
			m_exportedItems.Add(new Entry(src, dst));
		}

		public void AddErrorEntry(string src, string dst, string reason) {
			m_failedItems.Add(new ErrorEntry(src, dst, reason));
		}
	}
}

