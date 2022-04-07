# Ably Unity SDK
- Supports both [Mono](https://docs.unity3d.com/Manual/Mono.html) and [IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html) builds.
- Follow [SETUP.md](SETUP.md) doc for dev setup on the local.
  
### Downloading Unity Package
- Please download the latest Unity package from the GitHub releases page (https://github.com/ably/ably-dotnet/releases). All releases from 1.1.16 will include a Unity package as well.

### Importing Unity Package
- Import package by going to Assets -> Import Package -> Custom Package.
  For detailed information, visit https://docs.unity3d.com/Manual/AssetPackagesImport.html
- Make sure to [disable assembly validation](SETUP.md#disable-assembly-validation-error) if it fails due to conflict with internal newtonsoft json library.
- Please set `ClientOptions.AutomaticNetworkStateMonitoring` to `false` in the code, since the feature is not supported and throws runtime exception.
- Custom [NewtonSoft JSON DLLs](https://github.com/jilleJr/Newtonsoft.Json-for-Unity) under `Plugins` can be removed, in case of conflict with other NewtonSoft DLLs in the project or use of [inbuilt Newtonsoft](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html) is preferred.
  