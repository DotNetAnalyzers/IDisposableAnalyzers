# IDISP008
## Don't assign member with injected and created disposables

| Topic    | Value
| :--      | :--
| Id       | IDISP008
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [AssignmentAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/AssignmentAnalyzer.cs)
|          | [FieldAndPropertyDeclarationAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/FieldAndPropertyDeclarationAnalyzer.cs)

## Description

Don't assign member with injected and created disposables It creates a confusing ownership situation.

## Motivation

### Example 1
```cs
using System;
using System.Threading;

public class Foo : IDisposable
{
    private readonly Mutex dependency;

    public Foo(Mutex dependency = null)
    {
        this.dependency = dependency ?? new Mutex();
    }
}
```
In the above example the field `dependency` is either assigned with the constructor parameter or `new Mutex()` if null. This means that we don't know if we created the mutex and are responsible for disposing it.

### Example 2

```c#
using System.IO;

public class Foo
{
    public Stream Stream { get; set; } = File.OpenRead(string.Empty);
}
```

Above is a confusing situation about who is responsible for disposing the stream. 
It is created in the initializer but can later be assigned with streams created outside as there is no way to know what the property is assigned with.
In this case removing the `set;` makes analysis trivial, then we only need to check initializer and constructor to figure out what the property is assigned with.
Making it `private set` means we check all assignments within the class.

## How to fix violations

Make members holding created disposables readonly or at least private set.

#### Example public field
```cs
public IDisposable Disposable; // could be assigned from the outside so we don't know if disposing it is safe.
```
Change to 
```cs
public readonly IDisposable Disposable; // We can now check all places it is assigned and hopefully figure out if we should dispose
```

#### Example public property
```cs
public IDisposable Disposable { get; set; } // could be assigned from the outside so we don't know if disposing it is safe.
```
Change to 
```cs
public readonly IDisposable Disposable { get; } // We can now check all places it is assigned and hopefully figure out if we should dispose
```

For members accepting injected disposables never assign a disposable that we create inside the class.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP008 // Don't assign member with injected and created disposables
Code violating the rule here
#pragma warning restore IDISP008 // Don't assign member with injected and created disposables
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP008 // Don't assign member with injected and created disposables
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP008:Don't assign member with injected and created disposables", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->
