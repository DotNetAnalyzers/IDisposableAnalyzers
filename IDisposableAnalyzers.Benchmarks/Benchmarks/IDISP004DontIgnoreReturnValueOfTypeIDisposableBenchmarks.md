``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410097 Hz, Resolution=293.2468 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0


```
|                    Method |     Mean |     Error |   StdDev |    Gen 0 | Allocated |
|-------------------------- |---------:|----------:|---------:|---------:|----------:|
| RunOnIDisposableAnalyzers | 16.30 ms | 0.3867 ms | 1.140 ms | 156.2500 |   1.07 MB |
