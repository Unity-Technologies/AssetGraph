using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System;

using MiniJSONForAssetGraph;

/**
	sample class for finally hookPoint.

	read exported assetBundles & generate assetBundle data json as "EXPORT_PATH/bundleList.json".
*/
public class SampleFinally2 : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		
		// run only build time.
		if (!isBuild) return;

		var bundleInfos = new List<Dictionary<string, object>>();

		// get exported .manifest files from "Exporter" node.
		string targetNodeName = "Exporter";
		foreach (var groupKey in throughputs[targetNodeName].Keys) {
			foreach (var result in throughputs[targetNodeName][groupKey]) {
				// ignore SOMETHING.ASSET
				if (!result.EndsWith(".manifest")) continue;

				// ignore PLATFORM.manifest file.
				if (result.EndsWith(EditorUserBuildSettings.activeBuildTarget.ToString() + ".manifest")) continue;

				// get bundle info from .manifest file.
				var bundleInfo = GetBundleInfo(result);
				bundleInfos.Add(bundleInfo);
			}
		}

		var bundleListJson = Json.Serialize(bundleInfos);

		Debug.Log(bundleListJson);
	}

	/**
		get bundle information from BUNDLE.manifest.
	*/
	private Dictionary<string, object> GetBundleInfo (string manifestPath) {
		var bundle_name = Path.GetFileNameWithoutExtension(manifestPath);
		var bundlePath = manifestPath.Replace(".manifest", string.Empty);

		/*
			read SOMETHING.ASSETBUNDLE.manifest file then read manifest yaml data.
		*/
		var yamlStr = string.Empty;
		using (var sr = new StreamReader(manifestPath, true)) {
			yamlStr = sr.ReadToEnd();
		}

		// deserialize yaml.
		var input = new StringReader(yamlStr);
		var deserializer = new Deserializer(objectFactory: null, namingConvention: new PascalCaseNamingConvention(), ignoreUnmatched: true);

		var crcAndAssetsData = deserializer.Deserialize<CrcAndAssetsData>(input);

		/*
			generate bundle info.
		*/
		var bundleInfoDict = new Dictionary<string, object> {
			{"bundle_name", bundle_name},
			{"version", 0},
			{"size", new FileInfo(bundlePath).Length},
			{"crc", crcAndAssetsData.crc},
			{"resource_names", crcAndAssetsData.assets}
		};
		
		return bundleInfoDict;
	}

	public class CrcAndAssetsData {
		public long crc { get; set; }
		public List<string> assets { get; set; }
	}
}