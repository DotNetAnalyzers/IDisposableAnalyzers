``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0


```
|                    Method |     Mean |    Error |   StdDev |    Gen 0 | Allocated |
|-------------------------- |---------:|---------:|---------:|---------:|----------:|
| RunOnIDisposableAnalyzers | 125.6 ms | 2.509 ms | 6.107 ms | 625.0000 |    4.1 MB |
