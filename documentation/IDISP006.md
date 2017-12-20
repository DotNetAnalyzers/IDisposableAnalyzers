# IDISP006
## Implement IDisposable.

<!-- start generated table -->
<table>
<tr>
  <td>CheckId</td>
  <td>IDISP006</td>
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
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers.Analyzers/FieldAndPropertyDeclarationAnalyzer.cs">FieldAndPropertyDeclarationAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

The member is assigned with a created `IDisposable`s within the type. Implement IDisposable and dispose it.

## Motivation

The type creates IDisposable(s) and assigns it to a member. Hence it must implement IDisposable and dispose the member.

## How to fix violations

Us ethe code fixes or manually implement `IDisposable`

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP006 // Implement IDisposable.
Code violating the rule here
#pragma warning restore IDISP006 // Implement IDisposable.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP006 // Implement IDisposable.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP006:Implement IDisposable.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->