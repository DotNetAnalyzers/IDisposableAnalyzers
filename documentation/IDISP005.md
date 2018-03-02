# IDISP005
## Return type should indicate that the value should be disposed.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP005</td>
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
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers.Analyzers/ReturnValueAnalyzer.cs">ReturnValueAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Return type should indicate that the value should be disposed.

## Motivation

In the following method an IDisposable is created and returned but the api is not clear about that the caller should dispose the recieved value.

```C#
public object Meh()
{
    return File.OpenRead(string.Empty);
}
```

## How to fix violations

Use a returntype that is or implements `IDisposable`

```C#
public IDisposable Meh()
{
    return File.OpenRead(string.Empty);
}
```

or 

```C#
public Stream Meh()
{
    return File.OpenRead(string.Empty);
}
```
<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP005 // Return type should indicate that the value should be disposed.
Code violating the rule here
#pragma warning restore IDISP005 // Return type should indicate that the value should be disposed.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP005 // Return type should indicate that the value should be disposed.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP005:Return type should indicate that the value should be disposed.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->