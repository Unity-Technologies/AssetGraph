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

			private List<AssetBundleBuildReport> m_reports;

			public List<AssetBundleBuildReport> Reports {
				get {
					return m_reports;
				}
			}

			public AssetBundleBuildReportManager() {
				m_reports = new List<AssetBundleBuildReport>();
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

		static public void ClearBuildReports() {
			Manager.Reports.Clear();
		}

		static public void AddBuildReport(AssetBundleBuildReport r) {
			Manager.Reports.Add(r);
		}

		static public IEnumerable<AssetBundleBuildReport> BuildReports {
			get {
				return Manager.Reports;
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
}

