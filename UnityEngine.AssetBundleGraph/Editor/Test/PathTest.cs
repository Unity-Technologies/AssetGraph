#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

using UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

public class PathTest {

	[Test]
	public void PathTestSimplePasses() {
		// Use the Assert class to test conditions.

        string path;

        path = Settings.Path.BasePath;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));

        path = Settings.Path.ScriptTemplatePath;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));

        path = Settings.Path.SettingTemplatePath;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));

        path = Settings.Path.UserSpacePath;
        path = Settings.Path.CUISpacePath;
        path = Settings.Path.ImporterSettingsPath;
        path = Settings.Path.CachePath;
        path = Settings.Path.PrefabBuilderCachePath;
        path = Settings.Path.BundleBuilderCachePath;
        path = Settings.Path.SettingFilePath;
        path = Settings.Path.DatabasePath;
        path = Settings.Path.BuildMapPath;
        path = Settings.Path.BatchBuildConfigPath;

        path = Settings.Path.SettingTemplateModel;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.Path.SettingTemplateAudio;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.Path.SettingTemplateTexture;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.Path.SettingTemplateVideo;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.Path.GUIResourceBasePath;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));

        path = Settings.GUI.ConnectionPoint;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.GUI.InputBG;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.GUI.Skin;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
        path = Settings.GUI.OutputBG;
        Assert.AreNotEqual (string.Empty, AssetDatabase.AssetPathToGUID (path));
    }

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator PathTestWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}
}
#endif
