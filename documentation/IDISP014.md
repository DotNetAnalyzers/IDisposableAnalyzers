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
  <td>Enabled</td>
  <td>true</td>
</tr>
<tr>
  <td>Category</td>
  <td>IDisposableAnalyzers.Correctness</td>
</tr>
<tr>
  <td>TypeName</td>
  <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers.Analyzers/NodeAnalyzers/ObjectCreationAnalyzer.cs">ObjectCreationAnalyzer</a></td>
</tr>
</table>
<!-- end generated table -->

## Description

Use a single instance of HttpClient.

## Motivation

https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/

And:

http://byterot.blogspot.se/2016/07/singleton-httpclient-dns.html

## How to fix violations

```cs
private static HttpClient Client =new HttpClient { ... };
```

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
