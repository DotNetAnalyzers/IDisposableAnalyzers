# IDISP026
## Class with no virtual DisposeAsyncCore method should be sealed

| Topic    | Value
| :--      | :--
| Id       | IDISP026
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ClassDeclarationAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ClassDeclarationAnalyzer.cs)


## Description

Class with no virtual DisposeAsyncCore method should be sealed.

When implementing IAsyncDisposable, classes without a virtual DisposeAsyncCore method should be sealed.
See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync.

## How to fix violations

Mark classes the implement IAsyncDisposable as sealed.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP026 // Class with no virtual DisposeAsyncCore method should be sealed
Code violating the rule here
#pragma warning restore IDISP026 // Class with no virtual DisposeAsyncCore method should be sealed
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP026 // Class with no virtual DisposeAsyncCore method should be sealed
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP026:Class with no virtual DisposeAsyncCore method should be sealed", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->