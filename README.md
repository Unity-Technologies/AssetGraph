# AssetGraph

AssetGraph is a visual toolset lets you configure and create Unity's AssetBundles. It is intended to create rule-based configuration in visual form to create and manage efficient workflow around AssetBundle generation. 

**this is still alpha version. under development.**

##Usage
###1.Add Nodes
Right click AssetGraph canvas gives you list of nodes you can create. Select one of them and create nodes. To start, create Loader node to identify which assets should go into AssetBundles.
![SS](/Doc/1.png)

###2.Connect Them
When you create more than two nodes, let's connect them. Simply click the dot on created node, drag and drop to the dot of other node you wish to connect, then you will have link. 

AssetGraph gives you live update preview of asset list that will be passed through the link. By clicking link you will see the full list of assets.

![SS](/Doc/2.png)

###3.Configure Settings
By selecting a node, you can configure settings for your AssetBundle building rules. I.e. Filter node let you configure filtering rules, Importer node let you configure different importing setting you wish to apply onto assets go through that node. 
![SS](/Doc/3.png)

###4.Build It!
By pressing Build button on AssetGraph window, AssetBundles are built respect to rules you created.
Visual editor lets you build AssetBunldes in the way you want to while keeping everything easy, repeatable and scalable.
![SS](/Doc/4.png)  
![SS](/Doc/5.png)    
![SS](/Doc/6.png)

##Why Rule Based?
Because AssetGraph handles AssetBundle build pipeline by rules, programmers can safely build simple workflow with artists or game designers without making them worry about AssetBundle configuration. When they add new assets into project, AssetGraph automatically takes care of them and build necessary AssetBundles by your rule(s). 

##Nodes
There are several types of nodes you can use to construct AssetBundle building pipeline.

###Loader
Loader finds and lists assets. You can specify root directory of assets to target. You can also select directory outside /Assets/. 
- IN: none
- OUT: list of assets under given root directory

![SS](/Doc/1000.png)

###Filter
Filter filters list of assets passed by previous node. You can add multiple filtering rules to create multiple filter result.
- IN: list of assets
- OUT: list of assets which matches given filter setting

![SS](/Doc/600.png)  

###Importer
Importer overwrites import settings of assets passed by previous node for this AssetBundle build. (NOTE: original asset configuration remains. )
- IN: list of assets
- OUT: list of assets with given importer settings applied

![SS](/Doc/500.png)  

###Grouping
Grouping makes a group of resources from given list of assets by configured keyword.
"Group" is very useful approach for building AssetBundle. In keyword configuration, you can use *"*"* as a wildcard.
- IN: list of assets
- OUT: list of group of assets

![SS](/Doc/400-0.png)  
![SS](/Doc/400-1.png)  
![SS](/Doc/400-2.png)  

###Prefabricator
Prefabricator is a node that let you create Prefab in the form you need in your game. You can use Prefabricator by extending AssetGraph.PrefabricatorBase script and make your own Prefab.
- IN: list of group of assets
- OUT: list of group of assets (generated prefabs added to each group)

![SS](/Doc/700.png)  

#### Prefabricator code example:
```
public class CreateCharaPrefab : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir) {
		/*
			create character's prefab.

			1.texture & material -> set texture to the material of model.
			2.model -> instantiate, then set material to model.
			3.new prefab -> prefabricate model to new prefab.
			4.delete model instance from hierarchy.
		*/

		~~~ DO SOMETHING ~~~
		
		// export prefab data.
		PrefabUtility.ReplacePrefab(modelObj, prefabFile);
		
	}
}
```

full example script is [here](https://github.com/unity3d-jp/AssetGraph/blob/0.7.2/Assets/AssetGraph/Yours/Editor/CreateCharaPrefab.cs#L8).  


###Bundlizer
Bundlizer create "bundle" of given group of assets and configure generating AssetBundle's filename. "*" will be replaced to the grouping identifier.  
- IN: list of group of assets
- OUT: list of bundles

![SS](/Doc/800.png)


###BundleBuilder
BundleBuilder create actual AssetBundle files from given list of bundle configurations. By using Bundlizer and BundleBuilder(s), you can simultaneously create AssetBundles with different AssetBundle configuration (i.e. compressed & uncompressed)

- IN: list of bundles
- OUT: list of generated AssetBundle files

![SS](/Doc/100.png)


###Exporter
Exporter saves given assets into given directory.  You can also select directory outside /Assets/. 
- IN: list of assets (or AssetBundle files)

![SS](/Doc/900.png)
