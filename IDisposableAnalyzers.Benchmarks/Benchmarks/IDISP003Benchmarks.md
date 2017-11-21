``` ini

BenchmarkDotNet=v0.10.10, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
|                    Method |     Mean |    Error |   StdDev |     Gen 0 |    Gen 1 | Allocated |
|-------------------------- |---------:|---------:|---------:|----------:|---------:|----------:|
| RunOnIDisposableAnalyzers | 475.3 ms | 5.946 ms | 5.562 ms | 5937.5000 | 187.5000 |  35.93 MB |
