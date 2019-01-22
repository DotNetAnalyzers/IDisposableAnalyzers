# IDISP004
## Don't ignore return value of type IDisposable.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP004</td>
  </tr>
  <tr>
    <td>Severity</td>
    <td>Warning</td>
  </tr>
  <tr>
    <td>Enabled</td>
    <td>True</td>
  </tr>
  <tr>
    <td>Category</td>
    <td>IDisposableAnalyzers.Correctness</td>
  </tr>
  <tr>
    <td>Code</td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/IDISP004DontIgnoreCreated.cs">IDISP004DontIgnoreCreated</a></td>
  </tr>
  <tr>
    <td></td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ObjectCreationAnalyzer.cs">ObjectCreationAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Don't ignore return value of type IDisposable.

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
#pragma warning disable IDISP004 // Don't ignore return value of type IDisposable.
Code violating the rule here
#pragma warning restore IDISP004 // Don't ignore return value of type IDisposable.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP004 // Don't ignore return value of type IDisposable.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP004:Don't ignore return value of type IDisposable.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->
