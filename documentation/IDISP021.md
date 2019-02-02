# IDISP021
## Call this.Dispose(true).

| Topic    | Value
| :--      | :--
| Id       | IDISP021
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeMethodAnalyzer]([DisposeMethodAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs))

## Description

Call this.Dispose(true).

## Motivation

```cs
public class C : IDisposable
{
    public void Dispose()
    {
        this.Dispose(false); // should be true here
    }

    protected virtual void Dispose(bool disposing)
    {
        ...
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