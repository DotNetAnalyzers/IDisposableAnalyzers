# IDISP014
## Use a single instance of HttpClient.

| Topic    | Value
| :--      | :--
| Id       | IDISP014
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [ObjectCreationAnalyzer]([ObjectCreationAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/ObjectCreationAnalyzer.cs))

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
