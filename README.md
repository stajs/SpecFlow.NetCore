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

You can run manually from a command line with `dnu build` (or just call `dnx specflow-dnx` on it's own):

![image](https://cloud.githubusercontent.com/assets/2253814/11385431/a6922a22-937d-11e5-9dc4-c47cdeb95595.png)

### Supported frameworks

- `dnx46`

PRs to add support for other frameworks are welcome as long as they include an accompanying sample.

#### Samples

- https://github.com/stajs/SpecFlow.Dnx/tree/master/samples

If you build the samples, you should see `.feature.cs` files and an `app.config` being generated.

## Background

- https://github.com/techtalk/SpecFlow/issues/471
- https://github.com/techtalk/SpecFlow/issues/457
- https://groups.google.com/forum/#!topic/specflow/JTKdOTV5nII
