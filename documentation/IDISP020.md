# IDISP020
## Call SuppressFinalize with this.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP020</td>
  </tr>
  <tr>
    <td>Severity</td>
    <td>Warning</td>
  </tr>
  <tr>
    <td>Enabled</td>
    <td>True</td>
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

Call SuppressFinalize with this as argument.

## Motivation

```cs
public void Dispose()
{
    this.Dispose(true);
    GC.SuppressFinalize(null);
}
```

## How to fix violations

```cs
public void Dispose()
{
    this.Dispose(true);
    GC.SuppressFinalize(this);
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP020 // Call SuppressFinalize with this.
Code violating the rule here
#pragma warning restore IDISP020 // Call SuppressFinalize with this.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP020 // Call SuppressFinalize with this.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP020:Call SuppressFinalize with this.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->