# SpecFlow.NetCore

## The problem

As at the time of writing (September 2016), the `SpecFlow for Visual Studio 2015` extension does not play well with .NET Core projects (`.xproj`).

## The solution

Wait for the VS extension to support .NET Core projects. In the meantime, I present...

## The (hopefully temporary) solution

Update your `project.json`:

0. Include your testrunner of choice 

  * [xUnit](https://github.com/xunit/dotnet-test-xunit):

    ```json
    "dependencies": {
      "xunit": "2.1.0",
      "dotnet-test-xunit": "1.0.0-*"
    },
    "testRunner": "xunit"
    ```

  * [NUnit 3](https://github.com/nunit/dotnet-test-nunit) _(Experimental)_:
    ```json
    "dependencies": {
      "NUnit": "3.4.1",
      "dotnet-test-nunit": "3.4.0-beta-2"
    },
    "testRunner": "nunit"
    ```
  
  * [MsTest](https://www.nuget.org/packages/dotnet-test-mstest/1.1.1-preview) _(Experimental)_:
    ```json
    "dependencies": {
      "dotnet-test-mstest": "1.1.1-preview",
      "MSTest.TestFramework": "1.0.4-preview"
    },
    "testRunner": "mstest"
    ```

0. Include [`SpecFlow.NetCore`](https://www.nuget.org/packages/SpecFlow.NetCore):

  ```json
  "tools": {
    "SpecFlow.NetCore": "1.0.0-*"
  }
  ```

0. Add a `precompile` script:

  ```json
  "scripts": {
    "precompile": [ "dotnet SpecFlow.NetCore" ]
  }
  ```

0. Build for your tests to be discovered. **Note:** there is [a bug with the .NET Core CLI requiring a second build for newly added files to be discovered](https://github.com/stajs/SpecFlow.NetCore/issues/22).

### Samples

If you build the [samples](https://github.com/stajs/SpecFlow.NetCore/tree/master/samples/) solution, you should see `.feature.cs` files and an `app.config` being generated for each test framework.

## Supported frameworks

### .NET Core

- `netcoreapp1.0`
- `net46`
- `net461`

### Test frameworks

- [xUnit](https://xunit.github.io/)
- [NUnit 3](http://www.nunit.org/) - _Experimental support added by the community._
- [MsTest](https://blogs.msdn.microsoft.com/visualstudioalm/2016/05/30/announcing-mstest-framework-support-for-net-core-rc2-asp-net-core-rc2/) - _Experimental support added by the community._

## Visual Studio

### Test Explorer

![image](https://cloud.githubusercontent.com/assets/2253814/11646350/0a806578-9dc2-11e5-9abe-115616ec9aec.png)

<!--
## Generating step definitions

One of the nice features from the VS extension is being able to easily generate stubs for missing step definitions. This is still _kind_ of possible, but definitely not as nice as the typical usage from the extension.

0. So, a feature file:

  ![image](https://cloud.githubusercontent.com/assets/2253814/11574021/299d6d40-9a6e-11e5-9342-3cf9c91565cc.png)

0. Build to generate the `.feature.cs` file and run it:

  ![image](https://cloud.githubusercontent.com/assets/2253814/11574057/54f43bb8-9a6e-11e5-91d4-2910c1ee8185.png)

0. Right-click and `Copy All`:

  ![image](https://cloud.githubusercontent.com/assets/2253814/11574068/66050a5e-9a6e-11e5-9f7a-264c6935b3b6.png)

0. Paste in your text editor of choice, then copy out the actual steps:

  ![image](https://cloud.githubusercontent.com/assets/2253814/11574120/932672c0-9a6e-11e5-8f70-cff5a74c5da6.png)

Given this should be a short-lived solution, hopefully this workaround is tolerable.
-->

## Background

- [SpecFlow Issue 471](https://github.com/techtalk/SpecFlow/issues/471): Auto generation of `feature.cs` fails when using MSBuild that comes with VS2015
- [SpecFlow Issue 457](https://github.com/techtalk/SpecFlow/issues/457): SpecFlow "Generate Step Definition" context menu missing in VS2015
- [SpecFlow Google Group discussing VS2015 & DNX](https://groups.google.com/forum/#!topic/specflow/JTKdOTV5nII)
