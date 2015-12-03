# SpecFlow.Dnx

## The problem

As at the time of writing (November 2015), the `SpecFlow for Visual Studio 2015` extension does not play well with DNX projects (`.xproj`).

## The solution

Wait for the VS extension to support DNX projects. In the meantime, I present...

## The (hopefully temporary) solution

Update your `project.json`:

<!-- Resort to <pre> since markdown code blocks break the list numbering. -->
1. Include [`SpecFlow.Dnx`](https://www.nuget.org/packages/SpecFlow.Dnx):
<pre>
	"dependencies": {
		"SpecFlow.Dnx": "1.0.0-*"
	}
</pre>
2. Add a command:
<pre>
	"commands": {
		"specflow-dnx": "SpecFlow.Dnx"
	}
</pre>
3. Add a `prebuild` script to call your command:
<pre>
	"scripts": {
		"prebuild": [ "dnx specflow-dnx" ]
	}
</pre>

### Visual Studio

As of DNX RC1, [you have to "produce outputs"](https://github.com/aspnet/Home/issues/432) to pipe the build through `dnu`:

![image](https://cloud.githubusercontent.com/assets/2253814/11394282/f096a800-93c8-11e5-8b62-03d80cfb1b0e.png)

![image](https://cloud.githubusercontent.com/assets/2253814/11394338/4471d1a2-93c9-11e5-94d1-ac2744f77d84.png)

### Command line

You can run manually with `dnu build` (or just call `dnx specflow-dnx` on it's own):

![image](https://cloud.githubusercontent.com/assets/2253814/11385431/a6922a22-937d-11e5-9dc4-c47cdeb95595.png)

### Supported frameworks

- `dnx46`

PR's to add support for other frameworks are welcome as long as they include an accompanying sample.

#### Samples

If you build the [samples](https://github.com/stajs/SpecFlow.Dnx/tree/master/samples) solution, you should see `.feature.cs` files and an `app.config` being generated.

## Test Frameworks

The auto-generated `app.config` is configured to use [xUnit](https://xunit.github.io/):

```xml
<unitTestProvider name="xUnit" />
```

This can be changed as per the [SpecFlow Configuration Documentation](https://github.com/techtalk/SpecFlow/wiki/Configuration), however (at time of writing) no other test frameworks are available for DNX.

### Test Explorer

xUnit has been updated to work both with DNX and the Visual Studio Test Explorer. In order to make your project compatible with Test Explorer, please follow these steps:

1. Add a dependency to `xunit`. _(At time of writing, version `2.1.0`)_
2. Add a dependency to `xunit.runner.dnx`. _(At time of writing, version `2.1.0-rc1-build204`)_
3. Add a command to execute the xunit runner:

```json
"test": "xunit.runner.dnx"
```

> Note: It is important that the command used is "test". This is a convention used by Test Explorer, and will not work without it.

Here is a complete sample `project.json` for reference:

```json
{
	"version": "1.0.0-*",
	"dependencies": {
		"SpecFlow.Dnx": "1.0.0-alpha8",
		"xunit": "2.1.0",
		"xunit.runner.dnx": "2.1.0-rc1-build204"
	},
	"commands": {
		"create-specs": "SpecFlow.Dnx",
		"test": "xunit.runner.dnx"
	},
	"scripts": {
		"prebuild": "dnx create-specs"
	},
	"frameworks": {
		"dnx46": { }
	}
}
```

## Generating step definitions

One of the nice features from the VS extension is being able to easily generate stubs for missing step definitions. This is still _kind_ of possible, but definitely not as nice as the typical usage from the extension.

So, a feature file:

![image](https://cloud.githubusercontent.com/assets/2253814/11574021/299d6d40-9a6e-11e5-9342-3cf9c91565cc.png)

Build to generate the `.feature.cs` file and run it:

![image](https://cloud.githubusercontent.com/assets/2253814/11574057/54f43bb8-9a6e-11e5-91d4-2910c1ee8185.png)

Right-click and `Copy All`:

![image](https://cloud.githubusercontent.com/assets/2253814/11574068/66050a5e-9a6e-11e5-9f7a-264c6935b3b6.png)

Paste in your text editor of choice, then copy out the actual steps:

![image](https://cloud.githubusercontent.com/assets/2253814/11574120/932672c0-9a6e-11e5-8f70-cff5a74c5da6.png)

Given this should be a short-lived solution, this workaround might be enough.

## Background

- [SpecFlow Issue 471](https://github.com/techtalk/SpecFlow/issues/471): Auto generation of `feature.cs` fails when using MSBuild that comes with VS2015
- [SpecFlow Issue 457](https://github.com/techtalk/SpecFlow/issues/457): SpecFlow "Generate Step Definition" context menu missing in VS2015
- [SpecFlow Google Group discussing VS2015 & DNX](https://groups.google.com/forum/#!topic/specflow/JTKdOTV
