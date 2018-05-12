# IDISP007
## Don't dispose injected.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP007</td>
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
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/IDISP007DontDisposeInjected.cs">IDISP007DontDisposeInjected</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Don't dispose disposables you do not own.

## Motivation

Disposing `IDisposables` that you have not created and do not own can be a bug.

## How to fix violations

Don't dispose them.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP007 // Don't dispose injected.
Code violating the rule here
#pragma warning restore IDISP007 // Don't dispose injected.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP007 // Don't dispose injected.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP007:Don't dispose injected.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->