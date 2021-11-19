# ably-unity

## Requirements
1. Unity - Unity game engine (Currently using LTS 2020.3.19f1) 
2. Unity script editor - Visual studio (Official IDE)/ Visual studio code (Editor)/ Rider (Intellij IDE)
3. Git - VCS for ably-dotnet

## Installation
**1.** **Unity -** 
- Install unity hub (https://unity3d.com/get-unity/download) to manage unity projects and different versions of unity


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