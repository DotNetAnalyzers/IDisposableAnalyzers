``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410087 Hz, Resolution=293.2477 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |           Mean |         Error |       StdDev |         Median |      Gen 0 |     Gen 1 |   Allocated |
|----------------------------------------------- |---------------:|--------------:|-------------:|---------------:|-----------:|----------:|------------:|
|                         IDISP001DisposeCreated |   638,516.5 us | 13,770.992 us | 28,439.49 us |   635,214.5 us |  6750.0000 |  125.0000 |  42666115 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable | 3,884,713.3 us | 59,156.636 us | 55,335.15 us | 3,878,467.6 us | 54187.5000 | 2062.5000 | 341182092 B |
|                    IDISP007DontDisposeInjected |     2,794.2 us |     55.301 us |    137.72 us |     2,735.0 us |          - |         - |     23755 B |
|                               ArgumentAnalyzer |   582,698.3 us | 11,258.547 us | 10,531.25 us |   581,833.2 us |  6625.0000 |  125.0000 |  41838694 B |
|                             AssignmentAnalyzer |   496,726.9 us |  9,705.156 us | 14,820.80 us |   498,829.2 us |  4625.0000 |   62.5000 |  29293495 B |
|                          DisposeMethodAnalyzer |       420.8 us |      8.308 us |     19.58 us |       418.2 us |          - |         - |       612 B |
|            FieldAndPropertyDeclarationAnalyzer |   189,023.2 us |  3,750.701 us |  9,946.33 us |   186,025.8 us |  1437.5000 |   62.5000 |   9269781 B |
|                         ObjectCreationAnalyzer |       636.7 us |     19.738 us |     58.20 us |       629.0 us |     1.9531 |         - |     18511 B |
|                            ReturnValueAnalyzer |   899,846.3 us | 17,618.874 us | 24,699.20 us |   893,984.6 us | 11125.0000 |  250.0000 |  70375932 B |
