``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 7 SP1 (6.1.7601.0)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
Frequency=3410097 Hz, Resolution=293.2468 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0
  DefaultJob : .NET Framework 4.7.1 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2558.0


```
|                                         Method |           Mean |        Error |        StdDev |      Gen 0 |     Gen 1 |   Allocated |
|----------------------------------------------- |---------------:|-------------:|--------------:|-----------:|----------:|------------:|
|                         IDISP001DisposeCreated |     3,007.7 us |     88.07 us |     259.67 us |    19.5313 |    3.9063 |    146139 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable | 3,901,938.8 us | 76,280.15 us | 143,272.61 us | 54000.0000 | 2125.0000 | 339919310 B |
|                    IDISP007DontDisposeInjected |     3,281.0 us |     82.77 us |     244.04 us |     3.9063 |         - |     44602 B |
|                               ArgumentAnalyzer |   630,859.8 us | 12,478.22 us |  19,055.57 us |  6750.0000 |  187.5000 |  42732062 B |
|                             AssignmentAnalyzer |   518,485.8 us | 10,193.40 us |  17,583.12 us |  5000.0000 |   62.5000 |  31624579 B |
|                          DisposeMethodAnalyzer |       490.9 us |     11.58 us |      34.14 us |          - |         - |       556 B |
|            FieldAndPropertyDeclarationAnalyzer |   191,840.6 us |  5,072.04 us |  13,798.86 us |  1500.0000 |   62.5000 |   9816614 B |
|                         ObjectCreationAnalyzer |       654.8 us |     19.84 us |      58.18 us |     2.9297 |         - |     25663 B |
|                            ReturnValueAnalyzer | 1,000,370.2 us | 19,951.83 us |  47,028.81 us | 11375.0000 |  375.0000 |  71807276 B |
