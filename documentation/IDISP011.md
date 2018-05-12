# IDISP011
## Don't return disposed instance.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP011</td>
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
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/NodeAnalyzers/ReturnValueAnalyzer.cs">ReturnValueAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Don't return disposed instance.

## Motivation

ADD MOTIVATION HERE

## How to fix violations

ADD HOW TO FIX VIOLATIONS HERE

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP011 // Don't return disposed instance.
Code violating the rule here
#pragma warning restore IDISP011 // Don't return disposed instance.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP011 // Don't return disposed instance.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP011:Don't return disposed instance.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->