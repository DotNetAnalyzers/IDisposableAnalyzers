# IDISP012
## Property should not return created disposable.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP012</td>
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
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ReturnValueAnalyzer.cs">ReturnValueAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Property should not return created disposable.

## Motivation

ADD MOTIVATION HERE

## How to fix violations

ADD HOW TO FIX VIOLATIONS HERE

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP012 // Property should not return created disposable.
Code violating the rule here
#pragma warning restore IDISP012 // Property should not return created disposable.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP012 // Property should not return created disposable.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP012:Property should not return created disposable.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->