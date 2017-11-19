``` ini

BenchmarkDotNet=v0.10.10, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
|                    Method |     Mean |     Error |    StdDev |   Median |   Gen 0 | Allocated |
|-------------------------- |---------:|----------:|----------:|---------:|--------:|----------:|
| RunOnIDisposableAnalyzers | 4.365 ms | 0.1132 ms | 0.3320 ms | 4.275 ms | 31.2500 | 214.44 KB |
