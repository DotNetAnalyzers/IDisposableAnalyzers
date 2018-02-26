# IDISP014
## Use a single instance of HttpClient.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP014</td>
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
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers.Analyzers/ObjectCreationAnalyzer.cs">ObjectCreationAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Use a single instance of HttpClient.

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
#pragma warning disable IDISP014 // Use a single instance of HttpClient.
Code violating the rule here
#pragma warning restore IDISP014 // Use a single instance of HttpClient.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP014 // Use a single instance of HttpClient.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP014:Use a single instance of HttpClient.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->