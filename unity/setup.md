# **Ably-Unity Project Setup**
  Clone the `ably-dotnet` repo. and open `unity` folder. 

## **Requirements**
1. Git - VCS for ably-dotnet
2. Unity - Unity game engine (Currently using LTS 2020.3.19f1) 
3. Unity script editor - Visual studio (Official IDE)/ Visual studio code (Editor)/ Rider (Intellij IDE)
4. Basics of unity ![Official Unity Manual](https://docs.unity3d.com/Manual/index.html) and ![Unity Beginner Manual](https://docs.google.com/document/d/1TJSKoqsElTjnAVKy-nFcARGHthwv18PFdt8pnuorxvw/edit?usp=sharing) (Ignore if already know) 

## **Installation**
**1.** **Git -**
- Download and install git binary from https://git-scm.com/downloads (ignore if already installed)
- Clone the code using `git clone https://github.com/ably/ably-dotnet`

**2.** **Unity -** 
- Install unity hub (https://unity3d.com/get-unity/download) to manage unity projects and different versions of unity
- Open unity hub after installation.

1. Unity projects - 
- Unity projects are visible as a part of Unity Hub (list should be empty for none active projects)
  
![](doc_images/unity_projects.PNG)
- Please note, for each unity project, a specific unity version is specified, it can be changed by clicking on the visible version and selecting dropdown menu of installed unity versions.
  
2. Install specific unity version -   
- Click on **Installs** menu-item from left navigation bar to see the list of installed unity versions. Currently 2020.3.19f1 is installed, list should be empty for none installed.

![](doc_images/unity_versions.PNG)
- Click on **ADD** button on the top-right corner to install new version of unity.

![](doc_images/unity_add_version.PNG)
- Select most recent LTS version and click on next to install the selected unity version.
- Once installed, it should appear under `Installs` menu-item tab.

3. Install unity modules - 
- Unity modules are basically extra plugins to add build support for different platforms.
- Click on **`⋮`** to open menu for installing modules.
  
![](doc_images/unity_versions.PNG)

![](doc_images/unity_add_%20modules.PNG)

- Click on `Add Modules` option, to open list of modules

![](doc_images/unity_modules.PNG) 
- Select `Windows Build Support (IL2CPP)`  to add support for IL2CPP build support for windows, click on `DONE` to install the module

- For more information related to unity installation, please go through the ![official doc](https://docs.unity3d.com/Manual/UnityOverview.html) for ![Installing Unity](https://docs.unity3d.com/Manual/GettingStartedInstallingUnity.html), ![Unity Editor tabs/Windows](https://docs.unity3d.com/Manual/UsingTheEditor.html), ![Editor features](https://docs.unity3d.com/Manual/EditorFeatures.html) and ![Visual Studio Unity integration](https://docs.unity3d.com/Manual/VisualStudioIntegration.html)

**3.** **Visual studio -**
- Visual studio community is free to use so it can be downloaded and installed using ***visual studio installer***.
- Download official visual studio installer from https://visualstudio.microsoft.com/downloads/.
- ***Visual studio installer*** is used to manage different versions of visual studio along with necessary plugins/individual components for each version of visual studio. 
- Install and open visual studio installer.

1. Install visual studio  
- Two tabs are visible showing `Installed` and `Available` Visual studio versions.

![](doc_images/visual_studio_installer.PNG)  
- For none installed, it will open a new window showing visual studio components that needs to be installed
- Click on .Net Desktop development and check list of components on the right nav bar.

![](doc_images/vsi_modify_1.PNG)

![](doc_images/vsi_individual_1.PNG)
- Click on Game development with Unity for adding unity editor scripting support

![](doc_images/vsi_modify_2.JPG)
- Go to `Individual components` tab and select following components

![](doc_images/vsi_individual_2.PNG)  
- Click on modify button on the right-bottom to install visual studio
- Follow https://www.youtube.com/watch?v=FBo5Cso-ufE for detailed information on visual studio installation.

2. Update/Repair/Modify installed visual studio
- Open visual studio installer, it should show installed visual studio as below

![](doc_images/visual_studio_installer.PNG)

- Click on `Modify` to open original component installation window as below.

![](doc_images/vsi_modify_1.PNG)
- Check missing components and click on `Modify` to install them again.

## **Setup** 
- After cloning the project, open unity-hub.

![](doc_images/unity_projects.PNG)
- Click on `ADD` button and open project under `ably-dotnet\unity` 
- Default project layout should look as below
  
![](doc_images/unity_default_layout.PNG)  
- In current layout, test runner is missing. Default layout is not so user-friendly, so we need to change it as per requirement.
- Unity custom layout is provided under `ably-dotnet\unity\unity_layout.wlt`
- From the top-right corner, click selector named `Default`, it should show the menu to change the layout.
  
![](doc_images/unity_change_layout.PNG)  
- Select `Load Layout From File` and choose file under `ably-dotnet\unity\unity_layout.wlt`. New layout should look like this.

![](doc_images/unity_custom_layout.PNG)  
- Now we have a test runner windows at bottom-left along with user-friendly editor layout.

### **Disable assembly validation error**
- Unity internally has `NewtonSoftJson` package with assembly naming conflicting with provided `NewtonSoftJson.JSON.dll`(https://github.com/jilleJr/Newtonsoft.Json-for-Unity) as a plugin.
- So, it gives assembly validation error in the console while building the project, to resolve issue, disable `Assembly Validation`
- Go to `Edit -> Project Settings -> Player -> Assembly Version Validation`, uncheck the box and click on apply.

![](doc_images/assembly_version_validation.PNG)

## **Running tests**

1. **Run editmode tests**

Via GUI 
- At the bottom-left corner in the test runner window, click on `EditMode` tab
- All the tests should be available and shown under `EditMode.dll`
- Double click on `EditMode.dll` to run the tests or `right click` and `Run` the tests

![](doc_images/editmode_tests.PNG)

Via console
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults editmode-results.xml -testPlatform editmode
```

2. **Run playmode tests**
   
Via GUI
- At the bottom-left corner in the test runner window, click on `PlayMode` tab
- All the tests should be available and shown under `PlayMode.dll`
- Double click on `PlayMode.dll` to run the tests or `right click` and `Run` the tests

![](doc_images/playmode_tests.PNG)  

Via console
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults playmode-results.xml -testPlatform playmode
```

## **Export unitypackage**
![Via GUI](https://docs.unity3d.com/Manual/AssetPackagesCreate.html) 
- Right click on `Assets` under `project` window tab at top-left corner.
- Click on `Export Package` -> `Export` to export the standalone unity package.

![](doc_images/export_package.PNG)

Via Console 
```bash
Unity.exe -batchmode -nographics -quit -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -exportPackage 'Assets' 'ably.unitypackage'
```
- More information related to Asset Package Export and Import -> https://docs.unity3d.com/Manual/AssetPackages.html

## **CI/CD using gameCI**
- Currently, unity project testing and packaging is done using ![gameCI](https://game.ci/)
- `Unity License File (ulf)` is created using gameCI provided github action and added as a `UNITY_LICENSE` github secret.
- To learn more about unity CI using gameCI github actions, follow ![gameCI github actions](https://game.ci/docs/github/getting-started)