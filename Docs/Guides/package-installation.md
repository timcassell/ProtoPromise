# Package Installation

## Nuget

Install from https://www.nuget.org/packages/ProtoPromise/

`dotnet add package ProtoPromise`

## Unity

<b>*Highly Recommended*</b> If you are using Unity 2020.2 or newer, you should also install the `ProtoPromise.Analyzer` package. It requires `Microsoft.CodeAnalysis.CSharp` to be installed (I recommend using https://github.com/xoofx/UnityNuGet to install it if you don't already have it installed). Go to the latest [release](https://github.com/timcassell/ProtoPromise/releases) and download `ProtoPromise.Analyzer.unitypackage` and import it into your project.

### Unity Package Manager

#### OpenUPM registry (recommended)

1. Open `Edit > Project Settings > Package Manager`
2. Add a new Scoped Registry:
```
Name: OpenUPM
URL:  https://package.openupm.com/
Scope(s): com.timcassell
```
3. Click `Save`
4. Open `Window > Package Manager`
5. Click `+`
6. Click `Add package by name`
7. Paste `com.timcassell.protopromise`
8. Click `Add`

#### Git Url

1. Open `Window > Package Manager`
2. Click `+`
3. Click `Add package from git URL`
4. Paste `https://github.com/timcassell/ProtoPromise.git?path=Package`
5. Click `Add`

Or add `"com.timcassell.protopromise": "https://github.com/timcassell/ProtoPromise.git?path=Package"` to `Packages/manifest.json`.

You may append `#vX.X.X` to use a specific version, for example `https://github.com/timcassell/ProtoPromise.git?path=Package#v3.4.0`.

### Unity Asset Store

Add to your assets from the Asset Store at https://assetstore.unity.com/packages/tools/integration/protopromise-181997.

### Download unitypackage from GitHub

Go to the latest [release](https://github.com/timcassell/ProtoPromise/releases) and download `ProtoPromise.unitypackage`. Import the unitypackage into your Unity project.