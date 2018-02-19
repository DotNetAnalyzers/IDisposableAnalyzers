``` ini

BenchmarkDotNet=v0.10.12, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                    Method |     Mean |     Error |    StdDev |   Median |    Gen 0 | Allocated |
|-------------------------- |---------:|----------:|----------:|---------:|---------:|----------:|
| RunOnIDisposableAnalyzers | 16.79 ms | 0.3336 ms | 0.7864 ms | 16.34 ms | 187.5000 |   1.26 MB |
