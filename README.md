# SpecFlow.Dnx

## The problem

As at the time of writing (November 2015), the `SpecFlow for Visual Studio 2015` extension does not play well with DNX projects (`.xproj`).

## The solution

Wait for the VS extension to support DNX projects. In the meantime, I present...

## The (hopefully temporary) solution

Update your `project.json`:

1. Include [`SpecFlow.Dnx`](https://www.nuget.org/packages/SpecFlow.Dnx):
```
"dependencies": {
	"SpecFlow.Dnx": "1.0.0-*"
},
```
2. Add a command:
```
"commands": {
	"specflow-dnx": "SpecFlow.Dnx"
},
```
3. Add a `prebuild` script to call your command:
```
"scripts": {
	"prebuild": [ "dnx specflow-dnx" ]
}
```

**Note:** As of DNX RC1, [`prebuild` scripts seem to be broken](https://github.com/stajs/SpecFlow.Dnx/issues/1). You can run manually from a command line with `dnu build` in the meantime.

### Samples

- https://github.com/stajs/SpecFlow.Dnx/tree/master/samples

## Background

- https://github.com/techtalk/SpecFlow/issues/471
- https://github.com/techtalk/SpecFlow/issues/457
- https://groups.google.com/forum/#!topic/specflow/JTKdOTV5nII
