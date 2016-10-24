#QuickStart Guide
AssetBundle Graph Tool release 1.0

#What is the AssetBundle Graph Tool?
The AssetBundle Graph Tool is a GUI tool that enables you to create Unity AssetBundles. It also lets you create and adjust assets that you want to include, and creates a flow that reflects the changes and settings you have made to the assets without having to write any code.

Anyway, let’s look at how we are going to use it.

###1.Create a node
Show the AssetBundle window by selecting “Open Graph Editor” from the Window menu’s AssetBundleGraph. By right-clicking within the window, you can select the node you want to create. First, let’s create a Loader. Loaders are nodes that load assets in Unity projects to the AssetBundle Graph Tool.
After creating a Loader, create a Filter node in the same way too. Filters are nodes that filter the input files.

![SS](/Doc/images/guide/1.png)

###2.Connect the nodes
When you have created two or more nodes, connect them by clicking on either circle on the ends of the nodes, drag and drop them to other nodes. 

When you have connected the nodes, assets will flow from the Loader node on the left to the Filter node on the right. The number in between the connecting lines indicates the number of assets in that flow.  The list of assets can be shown in the Inspector by clicking on the number.

![SS](/Doc/images/guide/2.png)

###3.Changing Node Settings
Left-click on the node to show Node Settings in the Inspector. Settings that are adjustable vary depending on the type of node. For example, with the Filter node, you can group assets by name, and with the ImportSetting node, you can change the import-time settings of imported assets. 

![SS](/Doc/images/guide/3.png)

### 4.Building
The process flow of connected nodes is started by clicking on the Build button in the AssetBundle Graph Tool.

You can turn assets into AssetBundles by using and connecting the BundleConfig node and BundleBuilder node. BundleConfig does the AssetBundle settings and BundleBuilder does the actual building process of AssetBundles.

![SS](/Doc/images/guide/4.png)  
![SS](/Doc/images/guide/5.png)    

##Development Background of AssetBundle Graph Tool
AssetBundle Graph Tool was created with the goal of enabling anyone and everyone to use AssetBundles, by making asset adjustments and settings visually accessible. By automatically generating AssetBundles based on filename rules, artists and games designers do not have to worry too much about them during development and will be able to create many AssetBundles automatically by using this tool. 

#Utilizing AssetBundle Graph Tool

##Create an AssetBundle from multiple assets
How to create AssetBundles by loading assets into the AssetBundle Graph Tool:

1.In the Loader, specify the directory you want to load the asset from
1.Connect it to BundleConfig and name it with ImportSetting
1.Connect it to BundleBuilder and generate the AssetBundle
1.Use the Exporter to copy the generated AssetBundle to the designated place 

![SS](/Doc/images/guide/h1.png)

##Create separate AssetBundles with multiple assets
Create individual AssetBundles by grouping multiple assets with the Grouping node:

1. In the Loader, specify the directory you want to load the asset from
1.Connect it to Grouping and create a group from a path name by specifying a pattern
1.Connect it to BundleConfig and name the AssetBundle
1.Connect it to BundleBuilder and generate the AssetBundle 
1.Use the Exporter to copy the generated AssetBundle to the designated place 

![SS](/Doc/images/guide/h2.png)

##How to change the import settings of assets automatically:

You can change the import settings of assets that go through nodes by using the ImportSetting node
1.Connect ImportSetting to asset outputs from other nodes and click the Modify Import Setting button in the Inspector to adjust the import settings.

![SS](/Doc/images/guide/h3.png)

You can reflect the settings on all assets that go through the ImportSetting node just by doing this.

If multiple kinds of assets are included (for example, textures and models), group the assets by type with Filter and connect the groups with individual ImportSetting nodes. If you are using assets which do not have importers, like Material and RenderTexture, you can use the Modifier node instead.

##How to create Prefabs from assets automatically

There are instances where you want to create Prefabs of enemy characters etc. by using the model data from artists and adding script. You can create Prefabs with PrefabBuilder. You have to write the script to turn it into a Prefab. This is an example:

```
public UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
	GameObject go = new GameObject(string.Format("MyPrefab{0}", groupKey));
	GUITexture t = go.AddComponent<GUITexture>();

	Texture2D tex = (Texture2D)objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));

	t.texture = tex;
	t.color = color;

	return go;
}
```

Select Menu > AssetBundleGraph > Create Node Script > PrefabBuilder Script and create a script. Implement a simple function like this, return the GameObject and it will be saved as a Prefab.

Groups of objects that are passed to the List are groups that were made with Grouping. PrefabBuilder only returns one GameObject per group.

##Running AssetBundle Graph Tool from the command line
You can also run the AssetBundle Graph Tool from the command line. Select Menu > AssetBundleGraph > Create CUI Tool to create a valid CUI script on the platform you are using. You can build AssetBundles for your specified platform from the command line by doing this. 

```
$> sh -e buildassetbundle.sh -target WebGL
```

##How to process after running a build
You can process the script after building by creating a Postprocess script. Select Menu > AssetBundleGraph > Create Node Script > Postprocess Script to create a Postprocess script.

#Types of Nodes:

##Loader
- OUT: All assets that are in the specified folder

Loads all Assets that are in the specified folder of the Loader path

![SS](/Doc/images/guide/n_loader.png)

##Filter
-IN: Assets
-OUT: Assets that match keyword

Extracts assets that match the filter settings. You can also enable multiple outputs by setting multiple filters.

![SS](/Doc/images/guide/n_filter.png)

##Import Setting

-IN: Groups of assets
-OUT: Groups of assets that came from IN

Change the import settings of the texture, model and audio assets.

![SS](/Doc/images/guide/n_importsetting.png)

You can only set one ImportSetting per type of asset. If you have input different kinds of assets or have a different kind of asset to the preset asset, an error message will be displayed.

##Modifier
-IN: Groups of assets
-OUT: Groups of assets that came from IN

Directly change the settings of assets that do not have importers. You can change the all settings of RenderTexture and Material except for texture, model and audio. If you use Modifier, generally, you will have to write your own Modifier Script because the changes you make will vary widely depending on the project.

![SS](/Doc/images/guide/n_modifier.png)

You can create a script for Modifier by selecting Menu > AssetBundleGraph > Create Node Script > Modifier Script. When defining a Modifier on your own, specify what you are going to change with the AssetBundleGraph.CustomModifier attribute. 

```
[AssetBundleGraph.CustomModifier("MyModifier", typeof(RenderTexture))]
public class MyModifier : AssetBundleGraph.IModifier {

	[SerializeField] private bool doSomething;

	// Test if asset is different from intended configuration 
	public bool IsModified (object asset) {
		return false;
	}

	// Actually change asset configurations. 
	public void Modify (object asset) {
	}

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {
		GUILayout.Label("MyModifier!");

		var newValue = GUILayout.Toggle(doSomething, "Do Something");
		if(newValue != doSomething) {
			doSomething = newValue;
			onValueChanged();
		}
	}

	// serialize this class to JSON 
	public string Serialize() {
		return JsonUtility.ToJson(this);
	}
}
```

##Grouping
IN:Groups of assets
OUT: Asset groups that were grouped by settings

Group assets with a keyword

![SS](/Doc/images/guide/n_grouping.png)

You can create multiple groups from asset paths by specifying the pattern you are using for grouping in Inspector. The name of the group will match. For example, when you have two assets like “Menu/English/GUI.prefab", "Menu/Danish/GUI.prefab" and specify "Menu//" as the pattern, two groups named English and Danish will be created. 

##PrefabBuilder
-IN:Asset groups that will be materials for Prefabs
-OUT: Prefabs that include asset groups

You can create Prefabs from input assets with your specified script.  Input assets will generate a Prefab in addition to the output asset.

![SS](/Doc/images/guide/n_prefabbuilder.png)

You will have to create a simple script to use PrefabBuilder. You can create a script for Modifier by selecting Menu > AssetBundleGraph > Create Node Script > PrefabBuilder Script. The script looks like this:

```
[AssetBundleGraph.CustomPrefabBuilder("MyBuilder")]
public class MyPrefabBuilder : IPrefabBuilder {

	[SerializeField] private Color color;

	public string CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
		var tex = objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));

		if(tex != null) {
			return string.Format("MyPrefab{0}", groupKey);
		}

		return null;
	}

	public UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
		GameObject go = new GameObject(string.Format("MyPrefab{0}", groupKey));
		GUITexture t = go.AddComponent<GUITexture>();
		Texture2D tex = (Texture2D)objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));
		t.texture = tex;
		t.color = color;

		return go;
	}

	public void OnInspectorGUI (Action onValueChanged) {
		var newValue = EditorGUILayout.ColorField("Texture Color", color);
		if(newValue != color) {
			color = newValue;
			onValueChanged();
		}
	}

	public string Serialize() {
		return JsonUtility.ToJson(this);
	}
}
```

Once you have created the script, select and set the PrefabBuilder you want to use from the Inspector.

##BundleConfigurator
IN:Asset groups that you want to create AssetBundles from 
OUT: Asset groups that have been set to become AssetBundles

Fill in the settings to create AssetBundles from the input group of assets. Specify the bundle names with Bundle Name Template.  Asterisks (*) in the template name will be replaced by the group name.

![SS](/Doc/images/guide/n_bundleconfig.png)

You can also set variants by using BundleConfigurator. You can also treat the input of groups as a variant.

##BundleBuilder
IN: Asset groups that have been set to become AssetBundles
OUT: Generated AssetBundle files and manifest (1 group)

Build the AssetBundle which is set in the BundleConfigurator. You can specify build options like whether or not to compress AssetBundles etc.

![SS](/Doc/images/guide/n_bundlebuilder.png)

BundleBuilder will only accept input from BundleConfigurator. Due to this, asset groups have to go through BundleConfigurator.

##Exporter
IN: Asset groups that you want to output
You can output files to specified paths. You can set output options like to display error messages when there is no output folder, or to spawn automatically. 

![SS](/Doc/images/guide/n_exporter.png)

#Postprocess

You can add things after the build process by creating a Postprocess script. A simple script that generates a build report made with Postprocess looks like this:

```
public class MyPostprocess : AssetBundleGraph.IPostprocess {
	public void Run (Dictionary<AssetBundleGraph.NodeData, Dictionary<string, List<AssetBundleGraph.Asset>>> assetGroups, bool isRun) {

		if (!isRun) {
			return;
		}

		Debug.Log("BUILD REPORT:");

		foreach (var node in assetGroups.Keys) {
			var result = assetGroups[node];

			StringBuilder sb = new StringBuilder();

			foreach (var groupKey in result.Keys) {
				var assets = result[groupKey];

				sb.AppendFormat("In {0}:\n", groupKey);

				foreach (var a in assets) {
					sb.AppendFormat("\t {0} {1}\n", a.path, (a.isBundled)?"[in AssetBundle]":"");
				}
			}

			Debug.LogFormat("Node:{0}\n---\n{1}", node.Name, sb.ToString());
		}
	}
}
```

##Connecting nodes
There are nodes that connect with each other and ones that don’t.

![SS](/Doc/images/guide/nodeconnectivity.png)
