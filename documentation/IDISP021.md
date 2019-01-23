# IDISP021
## Call this.Dispose(true).

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP021</td>
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

Call this.Dispose(true).

## Motivation

```cs
public class C : IDisposable
{
    private bool isDisposed = false;

    public void Dispose()
    {
        this.Dispose(false); // should be true here
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            this.isDisposed = true;
        }
    }
}
```

## How to fix violations

Use the code fix.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP021 // Call this.Dispose(true).
Code violating the rule here
#pragma warning restore IDISP021 // Call this.Dispose(true).
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP021 // Call this.Dispose(true).
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP021:Call this.Dispose(true).", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->