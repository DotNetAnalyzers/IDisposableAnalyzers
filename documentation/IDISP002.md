# IDISP002
## Dispose member.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP002</td>
</tr>
<tr>
  <td>Severity</td>
  <td>Warning</td>
</tr>
<tr>
  <td>Category</td>
  <td>IDisposableAnalyzers.Correctness</td>
</tr>
<tr>
  <td>TypeName</td>
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers.Analyzers/FieldDeclarationAnalyzer.cs">FieldDeclarationAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Dispose the member as it is assigned with a created `IDisposable`.

## Motivation

In the example below the file will be left open.

```c#
public class Foo
{
    private FileStream stream = File.OpenRead("file.txt");
}
```

## How to fix violations

Implement `IDisposable` and dispose the member.

```c#
public sealed class Foo : IDisposable
{
    private FileStream stream = File.OpenRead("file.txt");

    public void Dispose()
    {
        this.stream.Dispose();
    }
}
```

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP002 // Dispose member.
Code violating the rule here
#pragma warning restore IDISP002 // Dispose member.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP002 // Dispose member.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP002:Dispose member.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->