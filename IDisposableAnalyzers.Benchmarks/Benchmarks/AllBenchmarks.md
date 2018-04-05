``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |         Mean |        Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|-------------:|-------------:|----------:|---------:|-----------:|
|                         IDISP001DisposeCreated | 339,411.5 us |  6,727.66 us | 19,836.65 us | 2687.5000 | 125.0000 | 17035956 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable | 784,366.1 us | 15,577.92 us | 18,544.40 us | 8437.5000 | 312.5000 | 53565474 B |
|                    IDISP007DontDisposeInjected |   3,218.6 us |     91.75 us |    267.64 us |         - |        - |    23826 B |
|                               ArgumentAnalyzer | 651,243.7 us | 12,498.76 us | 16,251.92 us | 5937.5000 | 187.5000 | 37495975 B |
|                             AssignmentAnalyzer | 338,533.0 us |  6,250.39 us | 11,739.75 us | 1375.0000 |        - |  9065994 B |
|                          DisposeMethodAnalyzer |     473.7 us |     14.06 us |     41.46 us |         - |        - |      612 B |
|            FieldAndPropertyDeclarationAnalyzer | 165,050.2 us |  3,265.40 us |  7,029.11 us |  687.5000 |        - |  4619244 B |
|                         ObjectCreationAnalyzer |     631.8 us |     17.34 us |     50.03 us |    1.9531 |        - |    17663 B |
|                            ReturnValueAnalyzer | 471,556.4 us |  8,838.93 us |  8,681.01 us | 3812.5000 | 125.0000 | 24151400 B |
