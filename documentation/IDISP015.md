# IDISP015
## Member should not return created and cached instance.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP015</td>
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
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/MethodReturnValuesAnalyzer.cs">MethodReturnValuesAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Member should not return created and cached instance.

## Motivation

When calling the method below it is not obvious if we are responsible for disposing the returned instance.
Avoid mixing created and cached disposables.

```cs
public IDisposable Bar()
{
    if (condition)
    {
        return this.disposable;
    }

    return File.OpenRead(string.Empty);
}
```

## How to fix violations

Redesign.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP015 // Member should not return created and cached instance.
Code violating the rule here
#pragma warning restore IDISP015 // Member should not return created and cached instance.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP015 // Member should not return created and cached instance.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP015:Member should not return created and cached instance.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->