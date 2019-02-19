# IDISP013
## Await in using.

| Topic    | Value
| :--      | :--
| Id       | IDISP013
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ReturnValueAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ReturnValueAnalyzer.cs)

## Description

Await in using.

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
#pragma warning disable IDISP013 // Await in using.
Code violating the rule here
#pragma warning restore IDISP013 // Await in using.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP013 // Await in using.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP013:Await in using.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->