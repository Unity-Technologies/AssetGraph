using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AssetBundles
{
	public class AssetBundlesMenuItems
	{
		const string kCleanCache = "Assets/AssetBundles/Clean Cache On Play";
		const string kSimulationMode = "Assets/AssetBundles/Simulation Mode";

		[MenuItem(kCleanCache)]
		public static void ToggleCleanCache ()
		{
			AssetBundleManager.CleanCacheOnPlay = !AssetBundleManager.CleanCacheOnPlay;
		}

		[MenuItem(kCleanCache, true)]
		public static bool ToggleCleanCacheValidate ()
		{
			Menu.SetChecked(kCleanCache, AssetBundleManager.CleanCacheOnPlay);
			return true;
		}

		[MenuItem(kSimulationMode)]
		public static void ToggleSimulationMode ()
		{
			AssetBundleManager.SimulateAssetBundleInEditor = !AssetBundleManager.SimulateAssetBundleInEditor;
		}

		[MenuItem(kSimulationMode, true)]
		public static bool ToggleSimulationModeValidate ()
		{
			Menu.SetChecked(kSimulationMode, AssetBundleManager.SimulateAssetBundleInEditor);
			return true;
		}
		
		[MenuItem ("Assets/AssetBundles/Build AssetBundles")]
		static public void BuildAssetBundles ()
		{
			BuildScript.BuildAssetBundles();
		}
	}
}