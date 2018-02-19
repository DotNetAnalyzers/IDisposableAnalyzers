``` ini

BenchmarkDotNet=v0.10.12, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |         Mean |        Error |       StdDev |       Median |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|-------------:|-------------:|-------------:|----------:|---------:|-----------:|
|                             AssignmentAnalyzer | 158,822.9 us | 3,306.576 us |  8,416.29 us | 157,178.0 us |  937.5000 |        - |  6082546 B |
|                          DisposeMethodAnalyzer |     409.8 us |     8.087 us |     17.75 us |     406.8 us |         - |        - |      836 B |
|            FieldAndPropertyDeclarationAnalyzer | 124,107.7 us | 2,495.092 us |  7,238.72 us | 122,517.3 us |  625.0000 |        - |  4328904 B |
|                         IDISP001DisposeCreated |   4,101.4 us |   121.302 us |    353.84 us |   3,970.6 us |   31.2500 |        - |   223350 B |
|               IDISP003DisposeBeforeReassigning | 508,695.2 us | 9,962.769 us | 14,603.30 us | 504,292.6 us | 6375.0000 | 187.5000 | 40482964 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  17,534.7 us |   362.352 us |    967.19 us |  17,298.9 us |  187.5000 |        - |  1320669 B |
|                    IDISP007DontDisposeInjected |   2,551.8 us |    50.857 us |    127.59 us |   2,526.5 us |         - |        - |    23396 B |
|                            ReturnValueAnalyzer |   8,333.0 us |   174.165 us |    420.63 us |   8,084.2 us |   46.8750 |        - |   366058 B |
