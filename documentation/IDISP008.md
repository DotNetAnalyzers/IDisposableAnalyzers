# IDISP008
## Don't assign member with injected and created disposables.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP008</td>
</tr>
<tr>
  <td>Severity</td>
  <td>Warning</td>
</tr>
<tr>
  <td>Enabled</td>
  <td>true</td>
</tr>
<tr>
  <td>Category</td>
  <td>IDisposableAnalyzers.Correctness</td>
</tr>
<tr>
  <td>TypeName</td>
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/NodeAnalyzers/FieldAndPropertyDeclarationAnalyzer.cs">FieldAndPropertyDeclarationAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Don't assign member with injected and created disposables. It creates a confusing ownership situation.

## Motivation

```c#
using System.IO;

public class Foo
{
    public Stream Stream { get; protected set; } = File.OpenRead(string.Empty);
}
```

Above is a confusing situation about who is responsible for disposing the stream. 
It is created in the initializer but can later be assigned with streams created outside.

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

#### Example public field
```cs
public IDisposable Disposable { get; set; } // could be assigned from the outside so we don't know if disposing it is safe.
```
Change to 
```cs
public readonly IDisposable Disposable { get; } // We can now check all places it is assigned and hopefully figure out if we should dispose
```

For members accepting injected disposables never assign a disposable that we create inside tha class.



<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP008 // Don't assign member with injected and created disposables.
Code violating the rule here
#pragma warning restore IDISP008 // Don't assign member with injected and created disposables.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP008 // Don't assign member with injected and created disposables.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP008:Don't assign member with injected and created disposables.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->