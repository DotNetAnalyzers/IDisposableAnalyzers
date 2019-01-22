# IDisposableAnalyzers
Roslyn analyzers for IDisposable

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Gitter](https://badges.gitter.im/DotNetAnalyzers/IDisposableAnalyzers.svg)](https://gitter.im/DotNetAnalyzers/IDisposableAnalyzers?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build status](https://ci.appveyor.com/api/projects/status/nt35pbd1r08vj2m8/branch/master?svg=true)](https://ci.appveyor.com/project/JohanLarsson/idisposableanalyzers/branch/master)
[![NuGet](https://img.shields.io/nuget/v/IDisposableAnalyzers.svg)](https://www.nuget.org/packages/IDisposableAnalyzers/)

* 1.x versions are for Visual Studio 2015.
* 2.x versions are for Visual Studio 2017.

<!-- start generated table -->
<table>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP001.md">IDISP001</a></td>
    <td>Dispose created.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP002.md">IDISP002</a></td>
    <td>Dispose member.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP003.md">IDISP003</a></td>
    <td>Dispose previous before re-assigning.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP004.md">IDISP004</a></td>
    <td>Don't ignore return value of type IDisposable.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP005.md">IDISP005</a></td>
    <td>Return type should indicate that the value should be disposed.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP006.md">IDISP006</a></td>
    <td>Implement IDisposable.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP007.md">IDISP007</a></td>
    <td>Don't dispose injected.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP008.md">IDISP008</a></td>
    <td>Don't assign member with injected and created disposables.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP009.md">IDISP009</a></td>
    <td>Add IDisposable interface.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP010.md">IDISP010</a></td>
    <td>Call base.Dispose(disposing)</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP011.md">IDISP011</a></td>
    <td>Don't return disposed instance.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP012.md">IDISP012</a></td>
    <td>Property should not return created disposable.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP013.md">IDISP013</a></td>
    <td>Await in using.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP014.md">IDISP014</a></td>
    <td>Use a single instance of HttpClient.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP015.md">IDISP015</a></td>
    <td>Member should not return created and cached instance.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP016.md">IDISP016</a></td>
    <td>Don't use disposed instance.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP017.md">IDISP017</a></td>
    <td>Prefer using.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP018.md">IDISP018</a></td>
    <td>Call SuppressFinalize.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP019.md">IDISP019</a></td>
    <td>Call SuppressFinalize.</td>
  </tr>
  <tr>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/documentation/IDISP020.md">IDISP020</a></td>
    <td>Call SuppressFinalize with this.</td>
  </tr>
  <tr>
    <td><a href="">SemanticModelCacheAnalyzer</a></td>
    <td>Controls if Semantic models should be cached for syntax trees.</td>
  </tr>
<table>
<!-- end generated table -->

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
