### F# project for building, testing and packaging ably-dotnet SDK

- This is a F# project created as per [Run-FAKE-using-a-dedicated-build-project](https://fake.build/guide/getting-started.html#Run-FAKE-using-a-dedicated-build-project).
- This contains scripts to build, test and package projects targetting multiple platforms. 
- Clone the project and download fake tools by running following command at root.
```
dotnet tool restore
```
- Running `.\build.cmd` will start the build process. By default it builds NetStandard project.

### Build Netframework
- We have a dedicated netframework project targeting .NetFramework 4.6+.
- Build NetFramework project 
```
./build.cmd Build.NetFramework
```

### Build NetStandard
- Netstandard currently supports explicit targets for netstandard2.0, net6.0 and net7.0.
- Build NetStandard project 
```
./build.sh Build.NetStandard
```

### Build Xamarin
- We have a xamarin solution targetting android and iOS. 
- Build Xamarin project
```
./build.sh Build.Xamarin
```
### Test Netframework
Run unit tests
```
./build.cmd Test.NetFramework.Unit 
./build.cmd Test.NetFramework.Unit.WithRetry // Retry failed tests
```
Run integration tests
```
./build.cmd Test.NetFramework.Integration
./build.cmd Test.NetFramework.Integration.WithRetry // Retry failed tests
```


### Test NetStandard
Run unit tests
```
./build.sh Test.NetStandard.Unit 
./build.sh Test.NetStandard.Unit.WithRetry // Retry failed tests
```
Run integration tests
```
./build.sh Test.NetStandard.Integration
./build.sh Test.NetStandard.Integration.WithRetry // Retry failed tests
```
- Additional `-f` flag can be supplied to test for target framework `net6.0` or `net7.0`
```
./build.sh Test.NetStandard.Unit -f net6.0 // run tests for .Net6.0 runtime
./build.sh Test.NetStandard.Unit -f net7.0 // run tests for .Net7.0 runtime
```

### Create Nuget packages
- Currently, we have two scripts to generate nuget packages
1. package.sh => 
- Responsible for creating core `ably.io` nuget package.
```
./package.sh 1.2.3 
```
- Above command creates `ably.io.1.2.3.nupkg` package at root.
- During release process, this package is hosted on [nuget-ably.io](https://www.nuget.org/packages/ably.io).

2. package-push.sh =>
- Responsible for creating push packages for android and iOS.
- Please take a look at [Push Notif Doc](../PushNotifications.md) for usage.
```
./package-push.sh 1.2.3 
```
- Above command creates `ably.io.push.android.1.2.3.nupkg` and `ably.io.push.ios.1.2.3` package at root.
- During release process, this package is hosted on [nuget-ably.io.push.android](https://www.nuget.org/packages/ably.io.push.android) and [nuget-ably.io.push.ios](https://www.nuget.org/packages/ably.io.push.ios).

### Development
- Please refer to [Getting Started](https://fake.build/guide/what-is-fake.html) for detailed documentation.
- Format `build.fs` by running following command at root. 

```
dotnet fantomas .\build-script\build.fs
```
