# IDISP001
## Dispose created.

| Topic    | Value
| :--      | :-- |
| Id       | IDISP001
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ArgumentAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ArgumentAnalyzer.cs)
|          | [AssignmentAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/AssignmentAnalyzer.cs)
|          | [IDISP001DisposeCreated](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/IDISP001DisposeCreated.cs)

## Description

When you create an instance of a type that implements `IDisposable` you are responsible for disposing it.
This rule will warn even if you have an explicit dispose call and try finally.
The reason for not filtering out those cases is that using reads better.

## Motivation

The following example will leave the file open.
```c#
var reader = new StreamReader("file.txt");
return reader.ReadLine();
```

## How to fix violations

```c#
using (var reader = new StreamReader("file.txt"))
{
    return reader.ReadLine();
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP001 // Dispose created.
Code violating the rule here
#pragma warning restore IDISP001 // Dispose created.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP001 // Dispose created.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP001:Dispose created.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->