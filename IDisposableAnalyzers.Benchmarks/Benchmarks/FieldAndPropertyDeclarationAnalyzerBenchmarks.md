``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                    Method |     Mean |    Error |   StdDev |    Gen 0 | Allocated |
|-------------------------- |---------:|---------:|---------:|---------:|----------:|
| RunOnIDisposableAnalyzers | 166.2 ms | 3.745 ms | 10.86 ms | 750.0000 |   4.82 MB |
