``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0


```
|                    Method |     Mean |     Error |    StdDev |   Gen 0 |  Gen 1 | Allocated |
|-------------------------- |---------:|----------:|----------:|--------:|-------:|----------:|
| RunOnIDisposableAnalyzers | 4.166 ms | 0.0919 ms | 0.2637 ms | 31.2500 | 3.9063 |  216.1 KB |
