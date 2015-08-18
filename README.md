# AssetGraph

Automate asset generating from import to AssetBundle via GUI!

AssetGraph is the tool for generating Unity Assets by editing flow of resource's stream.

##Usage
###1.Add "node"
Put nodes to canvas.  
![SS](/Doc/1.png)

###2.Connect them
Let it connect!  
You can preview which resources will run on the connection.  
![SS](/Doc/2.png)

###3.Set Parameters for each nodes
Every nodes has parameters for customizing resources.  
These settings will reflect to the resources on streams!
![SS](/Doc/3.png)

###4.Build!
Resources will be customized by the nodes!  
It's easy, repeatable, scalable way for generating assets.  
![SS](/Doc/4.png)  
![SS](/Doc/5.png)    
![SS](/Doc/6.png)

##Want more same-ruled assets from another resources?
AssetGraph controls all modification processes.  
When you added new resources, the output results are fully modified by the setting of AssetGraph!  
No need to modify them one by one.

##Nodes for flow
There are nodes for constructing resource modification flow.

###Loader
Load resources to AssetGraph from outside of /Assets folder.  
![SS](/Doc/1.png)

###Filter
Split resource streams by keyword.  
It's useful for split flow of resources by resource's path.  
![SS](/Doc/600.png)  

###Importer
Set import settings directly!  
The settings will be applied to all resources which passes this node.  
![SS](/Doc/500.png)  

###Grouping
Grouping resources by keyword.  
"Group" is very useful approach for this tool.  
The keyword should contains "*" as wildcard.  
Outputs will be grouped dynamically  by keyword.  
![SS](/Doc/400-0.png)  
![SS](/Doc/400-1.png)  
![SS](/Doc/400-2.png)  

###Prefabricator
Generate prefabs by attaching script.  
This script should extend specific type:"AssetGraph.PrefabricatorBase".  
![SS](/Doc/700.png)  


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
Bundle inputted resources.  
You can set the name of the file of AssetBundle.  
"*" will be replaced to the grouping identifier.  
![SS](/Doc/800.png)


###BundleBuilder
Generate actual AssetBundles.  
This node will output AssetBundle files only.  
![SS](/Doc/100.png)


###Exporter
Export resources to outside of /Assets folder.  
![SS](/Doc/900.png)
