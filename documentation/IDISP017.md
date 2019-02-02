# IDISP017
## Prefer using.

| Topic    | Value
| :--      | :--
| Id       | IDISP017
| Severity | Warning
| Enabled  | True
| Category | IDisposableAnalyzers.Correctness
| Code     | [DisposeCallAnalyzer]([DisposeCallAnalyzer](https://github.com/DotNetAnalyzers/IDisposableAnalyzers/blob/master/IDisposableAnalyzers/Analyzers/DisposeCallAnalyzer.cs))

## Description

Prefer using.

## Motivation

```cs
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            var stream = File.OpenRead(string.Empty)
            var b = stream.ReadByte();
            stream.Dispose();
        }
    }
}
```

Prefer using in the code above. It is cleaner than adding try-finally. The code above will not dispose the file if the read fails.

## How to fix violations

```cs
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public void Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
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
#pragma warning disable IDISP017 // Prefer using.
Code violating the rule here
#pragma warning restore IDISP017 // Prefer using.
```

Or put this at the top of the file to disable all instances.
```C#
#pragma warning disable IDISP017 // Prefer using.
```

### Via attribute `[SuppressMessage]`.

```C#
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers.Correctness", 
    "IDISP017:Prefer using.", 
    Justification = "Reason...")]
```
<!-- end generated config severity -->