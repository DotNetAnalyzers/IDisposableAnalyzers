``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |         Mean |         Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|--------------:|-------------:|----------:|---------:|-----------:|
|                             AssignmentAnalyzer | 222,910.2 us |  4,320.450 us |  9,018.39 us |  937.5000 |        - |  6270964 B |
|                          DisposeMethodAnalyzer |     507.7 us |     12.773 us |     37.06 us |         - |        - |      840 B |
|            FieldAndPropertyDeclarationAnalyzer | 143,292.7 us |  2,858.604 us |  7,118.91 us |  625.0000 |        - |  4362188 B |
|                         IDISP001DisposeCreated |   5,238.2 us |    141.501 us |    417.22 us |   31.2500 |        - |   257973 B |
|               IDISP003DisposeBeforeReassigning | 706,683.7 us | 13,378.677 us | 14,315.03 us | 6812.5000 | 250.0000 | 43263818 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  41,330.6 us |  1,085.734 us |  3,201.31 us |  250.0000 |        - |  1760174 B |
|                    IDISP007DontDisposeInjected |   3,047.3 us |     61.723 us |    181.99 us |         - |        - |    23088 B |
|                         ObjectCreationAnalyzer |     222.9 us |      6.534 us |     19.26 us |         - |        - |      540 B |
|                            ReturnValueAnalyzer | 482,628.4 us |  9,331.546 us | 12,773.13 us | 4250.0000 | 187.5000 | 26859256 B |
