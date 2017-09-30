#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.AssetBundles.GraphTool;

using UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

public class PathTest {

	[Test]
	public void PathTestSimplePasses() {
		// Use the Assert class to test conditions.

        string baseDirName = Settings.Path.ToolDirName;
        Assert.IsFalse(string.IsNullOrEmpty(baseDirName));

        Assert.IsTrue(Settings.Path.BasePath.Contains(Settings.Path.ToolDirName));

        string basePath = Settings.Path.BasePath;
        this.TestPath(Path.Combine(basePath, "Editor/ScriptTemplate"), Settings.Path.ScriptTemplatePath);
        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate"), Settings.Path.SettingTemplatePath);

        this.TestPath(Path.Combine(basePath, "Generated/Editor"), Settings.Path.UserSpacePath);
        this.TestPath(Path.Combine(basePath, "Generated/CUI"), Settings.Path.CUISpacePath);
        this.TestPath(Path.Combine(basePath, "SavedSettings/ImportSettings"), Settings.Path.ImporterSettingsPath);
        this.TestPath(Path.Combine(basePath, "Cache"), Settings.Path.CachePath);
        this.TestPath(Path.Combine(basePath, "Cache/Prefabs"), Settings.Path.PrefabBuilderCachePath);
        this.TestPath(Path.Combine(basePath, "Cache/AssetBundles"), Settings.Path.BundleBuilderCachePath);
        this.TestPath(Path.Combine(basePath, "SettingFiles"), Settings.Path.SettingFilePath);
        this.TestPath(Path.Combine(basePath, "SettingFiles/AssetReferenceDB.asset"), Settings.Path.DatabasePath);
        this.TestPath(Path.Combine(basePath, "SettingFiles/AssetBundleBuildMap.asset"), Settings.Path.BuildMapPath);
        this.TestPath(Path.Combine(basePath, "SettingFiles/BatchBuildConfig.asset"), Settings.Path.BatchBuildConfigPath);

        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate"), Settings.Path.SettingTemplatePath);
        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate/setting.fbx"), Settings.Path.SettingTemplateModel);
        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate/setting.wav"), Settings.Path.SettingTemplateAudio);
        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate/setting.png"), Settings.Path.SettingTemplateTexture);
        this.TestPath(Path.Combine(basePath, "Editor/SettingTemplate/setting.m4v"), Settings.Path.SettingTemplateVideo);
        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources"), Settings.Path.GUIResourceBasePath);
        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources/ConnectionPoint.png"), Settings.GUI.ConnectionPoint);
        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources/InputBG.png"), Settings.GUI.InputBG);
        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources/NodeStyle.guiskin"), Settings.GUI.Skin);
        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources/OutputBG.png"), Settings.GUI.OutputBG);
    }

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator PathTestWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}

    private void TestPath(string expected, string path)
    {
        Assert.AreEqual(expected, path);
    }
}
#endif
