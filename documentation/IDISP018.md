# IDISP018
## Call SuppressFinalize

| Topic    | Value
| :--      | :--
| Id       | IDISP018
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeMethodAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeMethodAnalyzer.cs)

## Description

Call SuppressFinalize(this) as the type has a finalizer.

## Motivation

Call `GC.SuppressFinalize(this)` if the type has a finalizer.

## How to fix violations

Use the code fix.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP018 // Call SuppressFinalize
Code violating the rule here
#pragma warning restore IDISP018 // Call SuppressFinalize
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP018 // Call SuppressFinalize
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP018:Call SuppressFinalize", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->