``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


```
|                Method |     Mean |     Error |    StdDev |   Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |---------:|----------:|----------:|---------:|------:|------:|------:|----------:|
| RunOnValidCodeProject | 2.713 ms | 0.2719 ms | 0.7668 ms | 2.453 ms |     - |     - |     - |  58.75 KB |
