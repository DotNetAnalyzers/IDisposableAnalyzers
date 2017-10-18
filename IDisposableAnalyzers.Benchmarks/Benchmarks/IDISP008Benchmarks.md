``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0


```
 |                        Method |     Mean |    Error |   StdDev |   Median |    Gen 0 | Allocated |
 |------------------------------ |---------:|---------:|---------:|---------:|---------:|----------:|
 | RunOnIDisposableAnalyzers | 165.8 ms | 7.376 ms | 21.75 ms | 156.7 ms | 750.0000 |   4.95 MB |
