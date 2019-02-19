# IDISP009
## Add IDisposable interface.

| Topic    | Value
| :--      | :--
| Id       | IDISP009
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeMethodAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs)

## Description

The type has a Dispose method but does not implement `IDisposable`.

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
#pragma warning disable IDISP009 // Add IDisposable interface.
Code violating the rule here
#pragma warning restore IDISP009 // Add IDisposable interface.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP009 // Add IDisposable interface.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP009:Add IDisposable interface.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->