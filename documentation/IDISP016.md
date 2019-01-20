# IDISP016
## Don't use disposed instance.

<!-- start generated table -->
<table>
  <tr>
    <td>CheckId</td>
    <td>IDISP016</td>
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
    <td>Code</td>
    <td><a href="https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeCallAnalyzer.cs">DisposeCallAnalyzer</a></td>
  </tr>
</table>
<!-- end generated table -->

## Description

Don't use disposed instance.

## Motivation

Touching a disposed instance is often a bug as it should throw `ObjectDisposedException`.

```cs
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            var b = stream.ReadByte();
        }
    }
}
```

## How to fix violations

Dispose after last usage.

<!-- start generated config severity -->
## Configure severity

### Via ruleset file.

Configure the severity per project, for more info see [MSDN](https://msdn.microsoft.com/en-us/library/dd264949.aspx).

### Via #pragma directive.
```C#
#pragma warning disable IDISP016 // Don't use disposed instance.
Code violating the rule here
#pragma warning restore IDISP016 // Don't use disposed instance.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP016 // Don't use disposed instance.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP016:Don't use disposed instance.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->