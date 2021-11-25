# IDISP020
## Call SuppressFinalize(this)

| Topic    | Value
| :--      | :--
| Id       | IDISP020
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeMethodAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs)

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
#pragma warning disable IDISP020 // Call SuppressFinalize(this)
Code violating the rule here
#pragma warning restore IDISP020 // Call SuppressFinalize(this)
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP020 // Call SuppressFinalize(this)
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP020:Call SuppressFinalize(this)", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->