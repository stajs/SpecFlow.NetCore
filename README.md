:warning: **SpecFlow itself (and by extension this project) is currently limited to Windows platforms with .NET Framework v4.5.1+, or non-Windows with Mono.**

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
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.3.0" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="xunit.runner.visualstudio" Version="2.2.0" />
          <PackageReference Include="xunit" Version="2.2.0" />
        </ItemGroup>
        ```
    
    * [NUnit](https://github.com/nunit/dotnet-test-nunit) _(Experimental)_:
        ```xml
        <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.3.0" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="NUnit" Version="3.8.1" />
          <PackageReference Include="dotnet-test-nunit" Version="3.4.0-beta-2" />
        </ItemGroup>
        ```
    
    * [MsTest](https://www.nuget.org/packages/dotnet-test-mstest/1.1.1-preview) _(Experimental)_:
        ```xml
        <ItemGroup>
          <PackageReference Include="Microsoft.NET.Test.Sdk" Version="15.3.0" />
          <PackageReference Include="SpecFlow" Version="2.1.0" />
          <PackageReference Include="MSTest.TestAdapter" Version="1.1.18" />
          <PackageReference Include="MSTest.TestFramework" Version="1.1.18" />
        </ItemGroup>
        ```

2. Include [`SpecFlow.NetCore`](https://www.nuget.org/packages/SpecFlow.NetCore):

    ```xml
    <ItemGroup>
      <DotNetCliToolReference Include="SpecFlow.NetCore" Version="1.3.2" />
    </ItemGroup>
    ```

3. Add a precompile script:

    ```xml
    <Target Name="PrecompileScript" BeforeTargets="BeforeBuild">
      <Exec Command="dotnet SpecFlow.NetCore" />
    </Target>
    ```

4. Build for your tests to be discovered.

   Note: there is [a bug with the .NET Core CLI requiring a second build for newly added files to be discovered](https://github.com/stajs/SpecFlow.NetCore/issues/22).

## Cross platform using Mono

This has been tested on Windows, Ubuntu and macOS (High Sierra). It works in exactly the same way except it doesn’t use DotNetCli because it doesn’t work cross platform. Instead we call dotnet-SpecFlow.NetCore.exe directly from the package, this is why we need an extra PackageReference to SpecFlow.NetCore. 

**You also need to reference SpecFlow 2.2 or higher due to a [Mono specific bug in SpecFlow](https://github.com/techtalk/SpecFlow/issues/701).**   
  
  ```xml
  <PropertyGroup>
        <SpecFlowNetCoreVersion>1.3.2</SpecFlowNetCoreVersion>
  </PropertyGroup>
    
  <ItemGroup>
     <PackageReference Include="SpecFlow.NetCore" Version="$(SpecFlowNetCoreVersion)" />
     
     <PackageReference Include="SpecFlow" Version="2.2.0" />
     <PackageReference Include="xunit.runner.visualstudio" Version="2.2.0" />
     <PackageReference Include="xunit" Version="2.2.0" />
  </ItemGroup>

  <Target Name="PrecompileScript" BeforeTargets="BeforeBuild">
    <Exec Command="mono $(NuGetPackageRoot)specflow.netcore/$(SpecFlowNetCoreVersion)/lib/$(TargetFramework)/dotnet-SpecFlow.NetCore.exe" />
  </Target>
  ```

## .NET Core &amp; target frameworks

SpecFlow itself is currently limited to Windows platforms with full .NET Framework v4.5.1+. This means that two of the most common [target frameworks](https://docs.microsoft.com/en-us/dotnet/standard/frameworks) are unsupported:

- ~~`.NET Standard`~~ (unsupported)
- ~~`.NET Core Application`~~ (unsupported)
- `.NET Framework`

For `.NET Framework`, the following Target Framework Monikers (TFMs) are officially supported:

- `net46`
- `net461`

TFMs of `net451` and above should support SpecFlow and this project, but have not been officially tested.

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

## Samples

If you build the [samples](https://github.com/stajs/SpecFlow.NetCore/tree/master/samples/) solution, you should see `.feature.cs` files and an `app.config` being generated for each test framework.

## Background

- [SpecFlow Issue 471](https://github.com/techtalk/SpecFlow/issues/471): Auto generation of `feature.cs` fails when using MSBuild that comes with VS2015
- [SpecFlow Issue 457](https://github.com/techtalk/SpecFlow/issues/457): SpecFlow "Generate Step Definition" context menu missing in VS2015
- [SpecFlow Google Group discussing VS2015 & DNX](https://groups.google.com/forum/#!topic/specflow/JTKdOTV5nII)
