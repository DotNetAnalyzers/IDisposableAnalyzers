# IDISP011
## Don't return disposed instance.

| Topic    | Value
| :--      | :--
| Id       | IDISP011
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ReturnValueAnalyzer]([ReturnValueAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ReturnValueAnalyzer.cs))

## Description

Don't return disposed instance.

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
#pragma warning disable IDISP011 // Don't return disposed instance.
Code violating the rule here
#pragma warning restore IDISP011 // Don't return disposed instance.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP011 // Don't return disposed instance.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP011:Don't return disposed instance.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->