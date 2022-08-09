# Contributing

## Development Flow

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing
5. Push to the branch (`git push origin my-new-feature`)
6. Create a new Pull Request

## Building and Packaging

The build scripts are written using ```fake``` and need to be run on Windows with Visual Studio 2019 installed. Fake and nuget.exe can be installed via [chocolatey](https://chocolatey.org)

```shell
choco install fake
choco install nuget.commandline
```

Running `.\build.cmd` will start the build process and run the tests. By default it runs the NetFramework tests.
To run the Netcore build and tests you can run `.\build.cmd Test.NetStandard`

## Working from source

If you want to incorporate `ably-dotnet` into your project from source (perhaps to use a specific development branch) the simplest way to do so is to add references to the relevant ably-dotnet projects. The following steps are specific to Visual Studio 2019, but the principal should transfer to other IDEs

1. Clone this repository to your local system (`git clone --recurse-submodules https://github.com/ably/ably-dotnet.git`)
2. Open the solution you want to reference ably-dotnet from
3. In Solution Explorer right click the root node (it will be labelled Solution 'YourSolutionName')
4. Select Add > Existing Project from the context menu
5. Browse to the ably-dotnet repository and add ably-dotnet\src\IO.Ably.Shared\IO.Ably.Shared.shproj
6. Browse to the ably-dotnet repository and add the project that corresponds to your target platform, so if you are targeting .NET Framework (AKA Classic .NET) you would add ably-dotnet\src\IO.Ably.NETFramework\IO.Ably.NETFramework.csproj, if you are targeting .NET Core 2 then chose ably-dotnet\src\IO.Ably.NetStandard20\IO.Ably.NetStandard20.csproj and so on.
7. In any project that you want to use `ably-dotnet` you need to add a project reference, to do so:
    1. Find your project in Solution Explorer and expand the tree so that the Dependencies node is visible
    2. Right click Dependencies and select Add Reference
    3. In the dialogue that opens you should see a list of the projects in your solution. Check the box next to IO.Ably.NETFramework (or whatever version you are trying to use) and click OK.

## Spec

The dotnet library follows the Ably [Features spec](https://docs.ably.com/client-lib-development-guide/features/). To ensure it is easier to look up whether a spec item has been implemented or not; we add a Trait attribute to tests that implement parts of the spec. The convention is to add `[Trait("spec", "spec tag")]` to unit tests.

To get a list of all spec items that appear in the tests you can run a script located in the tools directory.
You need to have .NET Core 3.1 installed. It works on Mac, Linux and Windows. Run `dotnet fsi tools/list-test-categories.fsx`. It will produce a `results.csv` file which will include all spec items, which file it was found and on what line.

## Release process

This library uses [semantic versioning](http://semver.org/). For each release, the following needs to be done:

1. Create a release branch named in the form `release/1.2.3`.
2. Run [`github_changelog_generator`](https://github.com/skywinder/Github-Changelog-Generator) to automate the update of the [CHANGELOG](./CHANGELOG.md). Once the `CHANGELOG` update has completed, manually change the `Unreleased` heading and link with the current version number such as `v1.2.3`. Also ensure that the `Full Changelog` link points to the new version tag instead of the `HEAD`. Commit this change.
3. Update the version number and commit that change.
4. Create a release PR (ensure you include an SDK Team Engineering Lead and the SDK Team Product Manager as reviewers) and gain approvals for it, then merge that to `main`.
5. Run `package.cmd` to create the nuget package.
6. Run `nuget push ably.io.*.nupkg -Source https://www.nuget.org/api/v2/package` (a private nuget API Key is required to complete this step, more information on publishing nuget packages can be found [here](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package))
7. Against `main`, add a tag for the version and push to origin such as `git tag 1.2.3 && git push origin 1.2.3`.
8. Visit [https://github.com/ably/ably-dotnet/tags](https://github.com/ably/ably-dotnet/tags) and `Add release notes` for the release including links to the changelog entry.
9. Create the entry on the [Ably Changelog](https://changelog.ably.com/) (via [headwayapp](https://headwayapp.co/))
