``` ini

BenchmarkDotNet=v0.10.12, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |         Mean |         Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|--------------:|-------------:|----------:|---------:|-----------:|
|                             AssignmentAnalyzer | 189,959.7 us |  3,773.103 us |  7,086.80 us |  937.5000 |        - |  6255091 B |
|                          DisposeMethodAnalyzer |     439.6 us |      8.770 us |     24.88 us |         - |        - |      834 B |
|            FieldAndPropertyDeclarationAnalyzer | 127,042.8 us |  2,535.497 us |  6,544.93 us |  625.0000 |        - |  4346832 B |
|                         IDISP001DisposeCreated |   7,716.9 us |    248.866 us |    733.79 us |   46.8750 |        - |   413802 B |
|               IDISP003DisposeBeforeReassigning | 612,462.1 us | 12,079.644 us | 16,534.75 us | 6812.5000 | 187.5000 | 42965720 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  42,178.5 us |  1,318.961 us |  3,888.99 us |  312.5000 |        - |  2067382 B |
|                    IDISP007DontDisposeInjected |   2,763.4 us |     60.189 us |    177.47 us |         - |        - |    23072 B |
|                         ObjectCreationAnalyzer |     191.1 us |      3.854 us |     11.36 us |         - |        - |      538 B |
|                            ReturnValueAnalyzer | 443,599.0 us |  8,759.089 us | 12,838.96 us | 4250.0000 | 125.0000 | 27021911 B |
