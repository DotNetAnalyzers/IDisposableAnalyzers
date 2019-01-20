# IDISP019
## Call SuppressFinalize.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP019</td>
  </tr>
  <tr>
    <td>Severity</td>
    <td>Warning</td>
  </tr>
  <tr>
    <td>Enabled</td>
    <td>true</td>
  </tr>
  <tr>
    <td>Category</td>
    <td>IDisposableAnalyzers.Correctness</td>
  </tr>
  <tr>
    <td>Code</td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs">DisposeMethodAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Call SuppressFinalize as there is a virtual dispose method.

## Motivation

Call SuppressFinalize(this) as the type has a virtual dispoe method.
In case subclasses add finalizers.


## How to fix violations

Use the code fix.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP019 // Call SuppressFinalize.
Code violating the rule here
#pragma warning restore IDISP019 // Call SuppressFinalize.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP019 // Call SuppressFinalize.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP019:Call SuppressFinalize.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->