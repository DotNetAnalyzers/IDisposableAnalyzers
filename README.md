# IDisposableAnalyzers
Roslyn analyzers for IDisposable

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Gitter](https://badges.gitter.im/DotNetAnalyzers/IDisposableAnalyzers.svg)](https://gitter.im/DotNetAnalyzers/IDisposableAnalyzers?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/nt35pbd1r08vj2m8/branch/master?svg=true)](https://ci.appveyor.com/project/JohanLarsson/idisposableanalyzers/branch/master)
[![Build Status](https://dev.azure.com/DotNetAnalyzers/IDisposableAnalyzers/_apis/build/status/IDisposableAnalyzers-CI?branchName=master)](https://dev.azure.com/DotNetAnalyzers/IDisposableAnalyzers/_build/latest?definitionId=1&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/IDisposableAnalyzers.svg)](https://www.nuget.org/packages/IDisposableAnalyzers/)

![animation](https://user-images.githubusercontent.com/1640096/51797806-5efa7380-220a-11e9-918d-c1b39da79c38.gif)

* 3.x versions are for Visual Studio 2019.
* 2.x versions are for Visual Studio 2017.
* 1.x versions are for Visual Studio 2015.

| Id       | Title
| :--      | :--
| [IDISP001](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP001.md)| Dispose created
| [IDISP002](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP002.md)| Dispose member
| [IDISP003](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP003.md)| Dispose previous before re-assigning
| [IDISP004](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP004.md)| Don't ignore created IDisposable
| [IDISP005](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP005.md)| Return type should indicate that the value should be disposed
| [IDISP006](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP006.md)| Implement IDisposable
| [IDISP007](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP007.md)| Don't dispose injected
| [IDISP008](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP008.md)| Don't assign member with injected and created disposables
| [IDISP009](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP009.md)| Add IDisposable interface
| [IDISP010](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP010.md)| Call base.Dispose(disposing)
| [IDISP011](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP011.md)| Don't return disposed instance
| [IDISP012](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP012.md)| Property should not return created disposable
| [IDISP013](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP013.md)| Await in using
| [IDISP014](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP014.md)| Use a single instance of HttpClient
| [IDISP015](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP015.md)| Member should not return created and cached instance
| [IDISP016](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP016.md)| Don't use disposed instance
| [IDISP017](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP017.md)| Prefer using
| [IDISP018](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP018.md)| Call SuppressFinalize
| [IDISP019](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP019.md)| Call SuppressFinalize
| [IDISP020](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP020.md)| Call SuppressFinalize(this)
| [IDISP021](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP021.md)| Call this.Dispose(true)
| [IDISP022](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP022.md)| Call this.Dispose(false)
| [IDISP023](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP023.md)| Don't use reference types in finalizer context.
| [IDISP024](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP024.md)| Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer.
| [IDISP025](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP025.md)| Class with no virtual dispose method should be sealed.
| [SyntaxTreeCacheAnalyzer]()| Controls caching of for example semantic models for syntax trees

## Using IDisposableAnalyzers

The preferable way to use the analyzers is to add the nuget package [IDisposableAnalyzers](https://www.nuget.org/packages/IDisposableAnalyzers/)
to the project(s).

The severity of individual rules may be configured using [rule set files](https://msdn.microsoft.com/en-us/library/dd264996.aspx)
in Visual Studio 2015.

## Installation

IDisposableAnalyzers can be installed using:
- [Paket](https://fsprojects.github.io/Paket/) 
- NuGet command line
- NuGet Package Manager in Visual Studio.


**Install using the command line:**
```bash
paket add IDisposableAnalyzers --project <project>
```

or if you prefer NuGet
```bash
Install-Package IDisposableAnalyzers
```

## Updating

The ruleset editor does not handle changed IDs well, if things get out of sync you can try:

1) Close visual studio.
2) Edit the ProjectName.rulset file and remove the IDisposableAnalyzers element.
3) Start visual studio and add back the desired configuration.

Above is not ideal, sorry about this. Not sure if this is our bug.


## Current status

Early alpha, finds bugs in the code but there are also bugs in the analyzer. The analyzer will probably never be perfect as it is a pretty hard problem to solve but we can improve it one test case at the time.
Write issues for places where it should warn but does not or where it warns where there is no bug.
