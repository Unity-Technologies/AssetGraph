using UnityEngine;
using UnityEditor;
using System.Collections;
using AssetBundles;
using System.IO;

public class ABTest : MonoBehaviour {

	[MenuItem ("ABTest/TestBuildBundle")]
	static void TestBuildAB() {
		var buildMap = new AssetBundleBuild[3];

		// set bundle name and contained contents to map for No 0.
		{
			var paths = new string[1];
			paths[0] = "Assets/AssetBundleSample/SampleAssets/MyCube.prefab";

			buildMap[0].assetBundleName = "mycube-bundle";
			buildMap[0].assetNames = paths;
		}

		// No 1.
		{
			var paths = new string[1];
			paths[0] = "Assets/AssetBundleSample/SampleAssets/MyMaterial.mat";

			buildMap[1].assetBundleName = "mymat-bundle";
			buildMap[1].assetNames = paths;
		}

		// No 2.
		{
			var paths = new string[2];
			paths[0] = "Assets/AssetBundleSample/SampleAssets/UnityLogo.png";

			buildMap[2].assetBundleName = "mypic-bundle";
			buildMap[2].assetNames = paths;
		}

		// Choose the output path according to the build target.
		string outputPath = Path.Combine(Utility.AssetBundlesOutputPath,  Utility.GetPlatformName());
		if (!Directory.Exists(outputPath) )
			Directory.CreateDirectory (outputPath);

		BuildPipeline.BuildAssetBundles(outputPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXIntel);
	}

}
