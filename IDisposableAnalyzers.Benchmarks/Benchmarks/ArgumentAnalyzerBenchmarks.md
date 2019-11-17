``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


```
|                Method |     Mean |     Error |   StdDev |   Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|----------:|---------:|---------:|------:|------:|------:|----------:|
| RunOnValidCodeProject | 4.228 ms | 0.5687 ms | 1.668 ms | 3.446 ms |     - |     - |     - |  99.35 KB |
