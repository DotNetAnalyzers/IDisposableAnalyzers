``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
Frequency=3410097 Hz, Resolution=293.2468 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                    Method |     Mean |    Error |   StdDev |     Gen 0 |   Gen 1 | Allocated |
|-------------------------- |---------:|---------:|---------:|----------:|--------:|----------:|
| RunOnIDisposableAnalyzers | 204.7 ms | 4.074 ms | 11.08 ms | 1562.5000 | 62.5000 |   9.48 MB |
