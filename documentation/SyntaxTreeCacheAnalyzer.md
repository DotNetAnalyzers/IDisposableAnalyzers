# SyntaxTreeCacheAnalyzer
## Controls caching of for example Semantic models for syntax trees.

| Topic    | Value
| :--      | :--
| Id       | SyntaxTreeCacheAnalyzer
| Severity | Hidden
| Enabled  | True
| Category | Caching
| Code     | [SemanticModelCacheAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/SemanticModelCacheAnalyzer.cs)

## Description

Controls if Semantic models should be cached for syntax trees.
This can speed up analysis significantly but means Visual Studio uses more memory during compilation.

## Motivation

For enabling and disabling caching of semantic models. Default caching is enabled, this is nice for performance but uses more memory.
In huge solutions the memory usage may be an issue and this analyzer should be disabled.

## How to fix violations

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable SyntaxTreeCacheAnalyzer // Controls caching of for example Semantic models for syntax trees.
Code violating the rule here
#pragma warning restore SyntaxTreeCacheAnalyzer // Controls caching of for example Semantic models for syntax trees.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable SyntaxTreeCacheAnalyzer // Controls caching of for example Semantic models for syntax trees.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("Caching", 
    "SyntaxTreeCacheAnalyzer:Controls caching of for example Semantic models for syntax trees.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->