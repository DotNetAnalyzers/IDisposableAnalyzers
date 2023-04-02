# IDISP011
## Don't return disposed instance

| Topic    | Value
| :--      | :--
| Id       | IDISP011
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ReturnValueAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ReturnValueAnalyzer.cs)

## Description

Don't return disposed instance.

## Motivation

In the below example the `FileStream` is disposed and not usable

```cs
public FileStream M(string fileName)
{
    using var stream = File.OpenRead(fileName);
    return stream;
}
```

## How to fix violations

Don't dispose an instance that is returned. Caller is responsible for disposing.

```cs
public FileStream M(string fileName)
{
    return File.OpenRead(fileName);
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP011 // Don't return disposed instance
Code violating the rule here
#pragma warning restore IDISP011 // Don't return disposed instance
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP011 // Don't return disposed instance
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP011:Don't return disposed instance", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->