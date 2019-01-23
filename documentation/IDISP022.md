# IDISP022
## Call this.Dispose(false).

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP022</td>
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
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/FinalizerAnalyzer.cs">FinalizerAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Call this.Dispose(false).

## Motivation

```cs
public class C : IDisposable
{
    ~C()
    {
        this.Dispose(↓true); // should be false here.
    }

    public void Dispose()
    {
        this.Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        ...
    }
}
```

## How to fix violations

ADD HOW TO FIX VIOLATIONS HERE

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP022 // Call this.Dispose(false).
Code violating the rule here
#pragma warning restore IDISP022 // Call this.Dispose(false).
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP022 // Call this.Dispose(false).
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP022:Call this.Dispose(false).", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->