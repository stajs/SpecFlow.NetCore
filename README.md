# SpecFlow.NetCore

## The problem

As at the time of writing (September 2016), the `SpecFlow for Visual Studio 2015` extension does not play well with .NET Core projects.

## The solution

Wait for the VS extension to support .NET Core projects. In the meantime, I present...

## The (hopefully temporary) solution

Update your project:

1. Include SpecFlow and your test framework of choice:

    * [xUnit](https://github.com/xunit/dotnet-test-xunit):
        ```xml
        <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.0.0-preview-20170106-08" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="xunit" Version="2.2.0-beta5-build3474" />
          <PackageReference Include="xunit.runner.visualstudio" Version="2.2.0-beta5-build1225" />
        </ItemGroup>
        ```
    
    * [NUnit](https://github.com/nunit/dotnet-test-nunit) _(Experimental)_:
        ```xml
        <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.0.0-preview-20170106-08" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="NUnit" Version="3.4.1" />
         <PackageReference Include="dotnet-test-nunit" Version="3.4.0-beta-2" />
        </ItemGroup>
        ```
    
    * [MsTest](https://www.nuget.org/packages/dotnet-test-mstest/1.1.1-preview) _(Experimental)_:
        ```xml
        <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.0.0-preview-20170106-08" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="MSTest.TestFramework" Version="1.0.8-rc" />
          <PackageReference Include="MSTest.TestAdapter" Version="1.1.8-rc" />
        </ItemGroup>
        ```

2. Include [`SpecFlow.NetCore`](https://www.nuget.org/packages/SpecFlow.NetCore):

    ```xml
    <ItemGroup>
      <DotNetCliToolReference Include="SpecFlow.NetCore" Version="1.0.0-rc8" />
    </ItemGroup>
    ```

3. Add a `precompile` script:

    ```xml
    <Target Name="PrecompileScript" BeforeTargets="BeforeBuild">
      <Exec Command="dotnet SpecFlow.NetCore" />
    </Target>
    ```

4. Build for your tests to be discovered. 

### Notes

- There is [a bug with the .NET Core CLI requiring a second build for newly added files to be discovered](https://github.com/stajs/SpecFlow.NetCore/issues/22).
- Support for the .NET Core 1.0.0-rc4 tooling and `.csproj` was added by the community in February 2017 (thanks @richardjharding!) but has not yet been officially tested.

### Samples

If you build the [samples](https://github.com/stajs/SpecFlow.NetCore/tree/master/samples/) solution, you should see `.feature.cs` files and an `app.config` being generated for each test framework.

## Supported frameworks

### .NET Core

- ~~`netcoreapp1.0`~~ (see [#39](https://github.com/stajs/SpecFlow.NetCore/issues/39))
- `net46`
- `net461`

### Test frameworks

- [xUnit](https://xunit.github.io/)
- [NUnit](http://www.nunit.org/) - _Experimental support added by the community._
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
