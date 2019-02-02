# IDISP010
## Call base.Dispose(disposing)

| Topic    | Value
| :--      | :--
| Id       | IDISP010
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeMethodAnalyzer]([DisposeMethodAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs))

## Description

Call base.Dispose(disposing)

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
#pragma warning disable IDISP010 // Call base.Dispose(disposing)
Code violating the rule here
#pragma warning restore IDISP010 // Call base.Dispose(disposing)
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP010 // Call base.Dispose(disposing)
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP010:Call base.Dispose(disposing)", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->