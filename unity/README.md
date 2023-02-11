# Ably Unity SDK
- Unity support is currently in beta.
- Supports both [Mono](https://docs.unity3d.com/Manual/Mono.html) and [IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html) builds.

Considerations:
* We are actively working towards automated testing by integrating Unity Cloud Build into our .NET CI pipeline.
* Installation requires developers to import a custom Unity package that includes all of Ably's dependencies.

### Supported Platforms
- Windows, MacOS, Linux, Android and iOS.

### System Requirements
* Unity 2019.x.x or newer
* The following Unity Player settings must be applied:
  * Scripting Runtime Version should be '.NET 4.x Equivalent'
  * Api Compatibility Level should be '.NET Standard 2.0'

### Downloading Unity Package
- Please download the latest Unity package from the [GitHub releases page](https://github.com/ably/ably-dotnet/releases/latest). All releases from 1.2.4 has `.unitypackage` included.

### Importing Unity Package
- Import package by going to Assets -> Import Package -> Custom Package.
  For detailed information, visit https://docs.unity3d.com/Manual/AssetPackagesImport.html
- Make sure to [disable assembly validation](CONTRIBUTING.md#disable-assembly-validation-error) if it fails due to conflict with internal newtonsoft json library.
- Please set `ClientOptions.AutomaticNetworkStateMonitoring` to `false` in the code, since the feature is not supported and throws runtime exception.
- Custom [NewtonSoft JSON DLLs](https://github.com/jilleJr/Newtonsoft.Json-for-Unity) under `Plugins` can be removed, in case of conflict with other NewtonSoft DLLs in the project or use of [inbuilt Newtonsoft](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html) is preferred.
- [Configure SynchronizationContext](../README.md#executing-callbacks-on-mainui-thread) to execute callbacks on Main/UI thread.
- Sample code

```dotnet

```

### Unsupported Platforms
- WebGL: Due to incompatibility with Websockets.<br/>
Read [Direct Socket Access](https://docs.unity3d.com/2019.3/Documentation/Manual/webgl-networking.html) section under WebGL Networking.

### Contributing
- Please take a look at [contributing doc](CONTRIBUTING.md) for information related to local dev-setup, writing and running tests.
- Detailed information related to updating ably DLLs using [unity plug-ins](https://docs.unity3d.com/Manual/Plugins.html) is given at [updating-ably-unitypackage](CONTRIBUTING.md#updating-ably-unitypackage).
