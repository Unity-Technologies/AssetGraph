# AssetBundleGraphTool

![SS](/Doc/images/readme/graph.png)

AssetBundleGraphTool is a visual toolset lets you configure and create Unity's AssetBundles. It is intended to create rule-based configuration in visual form to create and manage efficient workflow around AssetBundle generation. 


##Usage
###1.Add Nodes
Right click AssetBundleGraphTool canvas gives you list of nodes you can create. Select one of them and create nodes. To start, create Loader node to identify which assets should go into AssetBundles.
![SS](/Doc/images/readme/1.png)

###2.Connect Them
When you create more than two nodes, let's connect them. Simply click the dot on created node, drag and drop to the dot of other node you wish to connect, then you will have link. 

AssetBundleGraphTool gives you live update preview of asset list that will be passed through the link. By clicking link you will see the full list of assets.

![SS](/Doc/images/readme/2.png)

###3.Configure Settings
By selecting a node, you can configure settings for your AssetBundle building rules. I.e. Filter node let you configure filtering rules, Importer node let you configure different importing setting you wish to apply onto assets go through that node. 
![SS](/Doc/images/readme/3.png)

###4.Build It!
By pressing Build button on AssetBundleGraphTool window, AssetBundles are built respect to rules you created.
Visual editor lets you build AssetBunldes in the way you want to while keeping everything easy, repeatable and scalable.
![SS](/Doc/4.png)

###5.Born with multiplatform
AssetBundleGraphTool can configure all Asset Bundle build settings ready for multiplatform. You can choose platform to build and overwrite settings where you want to customize per platforms.

![SS](/Doc/images/readme/5.png)
![SS](/Doc/images/readme/6.png)


##Why Rule Based?
Because AssetBundleGraphTool handles AssetBundle build pipeline by rules, programmers can safely build simple workflow with artists or game designers without making them worry about AssetBundle configuration. When they add new assets into project, AssetBundleGraphTool automatically takes care of them and build necessary AssetBundles by your rule(s). 




##Nodes tips
There are several types of nodes you can use to construct AssetBundle building pipeline.

###Loader
Loader is your starting point of building AssetBundles. Loader finds and give list assets to following nodes.  
- OUT: list of assets under given directory

![SS](/Doc/images/readme/1000.png)

###Filter
Filter creates sub-list of assets coming from previous node. You can add multiple filtering rules to create multiple sub-list.
- IN: list of assets
- OUT: list of assets which matches given filter setting

![SS](/Doc/images/readme/600.png)  

###ImportSetting
ImportSetting overwrites import settings of assets passed by previous node.
- IN: list of assets
- OUT: list of assets with given importer settings applied

![SS](/Doc/images/readme/500.png)  

###Modifier
Modifier modifies asset configuration directly. You can also create your own modifier by implementing IModifier.
- IN: list of group of assets
- OUT: list of group of assets.

![SS](/Doc/images/readme/1100.png)


###Grouping
Grouping makes a group of resources from list of assets by configured pattern.
"Group" is very useful approach to build AssetBundle.
- IN: list of assets
- OUT: list of group of assets

![SS](/Doc/images/readme/400.png)  

###PrefabBuilder
PrefabBuilder is a node let you create Prefab in the form you need in your game. 
- IN: list of group of assets
- OUT: list of group of assets with your prefab

![SS](/Doc/images/readme/700.png)  

###BundleConfig
BundleConfig create catalog of AssetBundle's contents from given group of assets. "*" will be replaced to the grouping identifier. You can also create variants with BundleConfig.
- IN: list of group of assets
- OUT: list of group of assets in AssetBundle name.

![SS](/Doc/images/readme/800.png)


###BundleBuilder
BundleBuilder create actual AssetBundle files from given list of bundle configurations. By using BundleConfig and BundleBuilder(s), you can simultaneously create AssetBundles with different AssetBundle configuration (i.e. compressed & uncompressed)

- IN: list of bundles
- OUT: list of generated AssetBundle files (including manifests)

![SS](/Doc/images/readme/100.png)


###Exporter
Exporter saves given assets into given directory.  You can also select directory outside /Assets/. 
- IN: list of assets (or AssetBundle files)

![SS](/Doc/images/readme/900.png)


#License

The MIT License (MIT)
Copyright (c) 2016 Unity Technologies

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
