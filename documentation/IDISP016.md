# IDISP016
## Don't use disposed instance.

| Topic    | Value
| :--      | :--
| Id       | IDISP016
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeCallAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeCallAnalyzer.cs)

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