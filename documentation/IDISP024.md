# IDISP024
## Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer

| Topic    | Value
| :--      | :--
| Id       | IDISP024
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [SuppressFinalizeAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/SuppressFinalizeAnalyzer.cs)


## Description

Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer.

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
#pragma warning disable IDISP024 // Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer
Code violating the rule here
#pragma warning restore IDISP024 // Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP024 // Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP024:Don't call GC.SuppressFinalize(this) when the type is sealed and has no finalizer", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->