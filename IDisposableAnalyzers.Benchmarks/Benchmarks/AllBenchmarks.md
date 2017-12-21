``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0


```
|                                         Method |         Mean |         Error |       StdDev |       Median |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|--------------:|-------------:|-------------:|----------:|---------:|-----------:|
|                          DisposeMethodAnalyzer |     391.7 us |      7.793 us |     14.64 us |     391.9 us |         - |        - |      836 B |
|            FieldAndPropertyDeclarationAnalyzer | 119,906.9 us |  2,392.264 us |  6,588.99 us | 118,615.1 us |  625.0000 |        - |  4241461 B |
|                         IDISP001DisposeCreated |   4,048.1 us |     80.628 us |    230.04 us |   3,985.1 us |   31.2500 |        - |   221188 B |
|               IDISP003DisposeBeforeReassigning | 527,654.5 us | 10,452.289 us | 21,817.82 us | 523,980.7 us | 6187.5000 | 187.5000 | 39216815 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  14,265.9 us |    456.105 us |  1,344.84 us |  14,061.8 us |  140.6250 |        - |   928906 B |
|                    IDISP007DontDisposeInjected |   2,522.2 us |     49.832 us |    128.63 us |   2,501.4 us |         - |        - |    23388 B |
|     IDISP008DontMixInjectedAndCreatedForMember |   1,421.2 us |     38.428 us |    112.70 us |   1,394.7 us |    5.8594 |        - |    44992 B |
|                            ReturnValueAnalyzer |   8,784.8 us |    221.953 us |    650.95 us |   8,649.3 us |   46.8750 |        - |   358147 B |
