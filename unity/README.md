# ably-unity
  Ably `unity` project is under `ably-dotnet` folder. Use unity-hub to open the project with LTS unity version.

## Requirements
1. Git - VCS for ably-dotnet
2. Unity - Unity game engine (Currently using LTS 2020.3.19f1) 
3. Unity script editor - Visual studio (Official IDE)/ Visual studio code (Editor)/ Rider (Intellij IDE)

## Installation
**1.** **Git -**
- Download and install git binary from here https://git-scm.com/downloads (ignore if already installed)
- Clone the code using `git clone https://github.com/ably/ably-dotnet`
- 
**2.** **Unity -** 
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

**3.** **Visual studio -**
- Visual studio community is free to use so it can be downloaded and installed using ***visual studio installer***.
- Install official visual studio installer from https://visualstudio.microsoft.com/downloads/.
- ***Visual studio installer*** is used to manage different versions of visual studio along with necessary plugins/individual components for each version of visual studio. 
- Install and open visual studio installer 

1. Install visual studio  
- Two tabs are visible showing `Installed` and `Available` Visual studio versions.
![](readme_images/visual_studio_installer.PNG)  
- For none installed, it will open a new window showing visual studio components that needs to be installed
- Click on .Net Desktop development and check list of components on the right nav bar.
![](readme_images/vsi_modify_1.PNG)
![](readme_images/vsi_individual_1.PNG)
- Click on Game development with Unity for adding unity editor scripting support
![](readme_images/vsi_modify_2.PNG)
- Go to `Individual components` tab and select following components
![](readme_images/vsi_individual_2.PNG)  
- Click on modify button on the right-bottom to install visual studio
- Follow https://www.youtube.com/watch?v=FBo5Cso-ufE for detailed information on visual studio installation.

2. Update/Repair/Modify installed visual studio
- Open visual studio installer, it should show installed visual studio as below
![](readme_images/visual_studio_installer.PNG)
- Click on `Modify` to open original component installation window as below.
![](readme_images/vsi_modify_1.PNG)
- Check missing components and click on `Modify` to install them again.

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