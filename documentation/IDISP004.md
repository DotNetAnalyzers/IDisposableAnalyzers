# IDISP004
## Don't ignore created IDisposable.

| Topic    | Value
| :--      | :--
| Id       | IDISP004
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [CreationAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/CreationAnalyzer.cs)

## Description

Don't ignore created IDisposable.

## Motivation

In the following code the file is opened but not closed.

```c#
public sealed class Foo
{
    public Foo()
    {
        File.OpenRead("file.txt");
    }
}
```

## How to fix violations

Assign the value to a field or property or use it in a using if it is a temporary value.


In the following code the file is opened but not closed.

```c#
public sealed class Foo
{
    public Foo()
    {
        using(var file = File.OpenRead("file.txt"))
        {
            ...
        }
    }
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
Code violating the rule here
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP004:Don't ignore created IDisposable.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->
