# IDISP007
## Don't dispose injected.

| Topic    | Value
| :--      | :-- 
| Id       | IDISP007
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeCallAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeCallAnalyzer.cs)
|          | [UsingStatementAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/UsingStatementAnalyzer.cs)

## Description

Don't dispose disposables you do not own.

## Motivation

Disposing `IDisposables` that you have not created and do not own can be a bug.

## How to fix violations

Don't dispose them.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP007 // Don't dispose injected.
Code violating the rule here
#pragma warning restore IDISP007 // Don't dispose injected.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP007 // Don't dispose injected.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP007:Don't dispose injected.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->