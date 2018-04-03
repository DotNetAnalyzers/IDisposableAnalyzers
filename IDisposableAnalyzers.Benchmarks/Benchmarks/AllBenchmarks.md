``` ini

BenchmarkDotNet=v0.10.13, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical cores and 8 physical cores
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |         Mean |        Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|-------------:|-------------:|----------:|---------:|-----------:|
|                         IDISP001DisposeCreated | 314,504.7 us |  6,227.67 us | 17,463.04 us | 2687.5000 | 125.0000 | 17035570 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable | 730,868.1 us | 17,339.28 us | 51,125.26 us | 8437.5000 | 250.0000 | 53560858 B |
|                    IDISP007DontDisposeInjected |   2,958.5 us |     80.92 us |    229.54 us |         - |        - |    23403 B |
|                               ArgumentAnalyzer | 641,594.5 us | 13,092.66 us | 36,928.07 us | 5937.5000 | 125.0000 | 37621636 B |
|                             AssignmentAnalyzer | 224,516.7 us |  4,473.16 us | 11,139.71 us |  875.0000 |        - |  5749736 B |
|                          DisposeMethodAnalyzer |     466.6 us |     10.62 us |     30.80 us |         - |        - |      612 B |
|            FieldAndPropertyDeclarationAnalyzer | 149,836.6 us |  5,063.22 us |  4,972.76 us |  687.5000 |        - |  4522446 B |
|                         ObjectCreationAnalyzer |     616.7 us |     20.14 us |     58.43 us |    1.9531 |        - |    17983 B |
|                            ReturnValueAnalyzer | 440,108.5 us |  8,793.71 us | 17,561.98 us | 3812.5000 | 187.5000 | 24166746 B |
