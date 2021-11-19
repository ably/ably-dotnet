# ably-unity
  Ably `unity` project is under `ably-dotnet` folder. Use unity editor to open thr project.

## Requirements
1. Unity - Unity game engine (Currently using LTS 2020.3.19f1) 
2. Unity script editor - Visual studio (Official IDE)/ Visual studio code (Editor)/ Rider (Intellij IDE)
3. Git - VCS for ably-dotnet

## Installation and setup
**1.** **Unity -** 
- Install unity hub (https://unity3d.com/get-unity/download) to manage unity projects and different versions of unity
- Open unity hub after installation.

1. Unity projects - 
- Unity projects are visible as below as a part of Unity Hub (list should be empty for none active projects)
![](readme_images/unity_projects.PNG)
- Please note, for each unity project, a specific unity version is specified, it can be changed by clicking on the visible version and selecting dropdown menu of installed unity versions.
  
1. Install specific unity version -   
- Click on **Installs** menu-item from left navigation bar to see the list of installed unity versions. Currently 2020.3.19f1 is installed, list should be empty for none installed.
![](readme_images/unity_versions.PNG)
- Click on **ADD** button on the top-right corner to install new version of unity.
![](readme_images/unity_add_version.PNG)
- Select most recent LTS version and click on next to install the selected unity version.
- Once installed, it should appear under `Installs` menu-item tab.

3. Install unity modules - 
- Unity modules are basically extra plugins to add build support for different platforms.
- Click on 3 dots next to installed unity version to open menu for installing modules
![](readme_images/unity_versions.PNG)
![](readme_images/unity_add_%20modules.PNG)
- Click on `Add Modules` option, to open list of modules
![](readme_images/unity_modules.PNG) 
- Select `Windows Build Support (IL2CPP)`  to add support for IL2CPP build support for windows, click on `DONE` to install the module

**2.** **Visual studio -**
- Install official LTS visual studio from https://visualstudio.microsoft.com/downloads/.
- Visual studio community is free to use so it can be downloaded and installed using ***visual studio installer***.
- ***Visual studio installer*** is used to manage different versions of visual studio along with necessary plugins/individual components for each version of visual studio. 


**3.** **Git -**
- Download and install git binary from here https://git-scm.com/downloads (ignore if already installed)
- Clone the code using `git clone https://github.com/ably/ably-dotnet`

## Running tests

1. Run editmode tests
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults editmode-results.xml -testPlatform editmode
```

2. Run playmode tests
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults playmode-results.xml -testPlatform playmode
```

## Export unitypackage
```bash
Unity.exe -batchmode -nographics -quit -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -exportPackage 'Assets' 'ably.unitypackage'
```