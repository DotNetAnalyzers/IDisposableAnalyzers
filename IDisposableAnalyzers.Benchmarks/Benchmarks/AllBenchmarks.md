``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
Frequency=3410073 Hz, Resolution=293.2489 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3260.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3260.0


```
|                              Method |          Mean |       Error |      StdDev |        Median | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------ |--------------:|------------:|------------:|--------------:|------------:|------------:|------------:|--------------------:|
|                    ArgumentAnalyzer |  3,348.785 us |  49.3458 us |  46.1581 us |  3,345.969 us |           - |           - |           - |             50328 B |
|                  AssignmentAnalyzer | 16,425.015 us | 143.3581 us | 127.0832 us | 16,379.268 us |           - |           - |           - |            475136 B |
|                 DisposeCallAnalyzer |  8,788.043 us | 145.9214 us | 136.4950 us |  8,811.248 us |           - |           - |           - |            163840 B |
|               DisposeMethodAnalyzer |  1,101.193 us |  21.8295 us |  49.2729 us |  1,095.871 us |           - |           - |           - |                   - |
| FieldAndPropertyDeclarationAnalyzer |  5,699.508 us | 118.0816 us | 131.2474 us |  5,652.665 us |           - |           - |           - |            131072 B |
|                   FinalizerAnalyzer |      3.680 us |   0.1506 us |   0.3995 us |      3.519 us |           - |           - |           - |                   - |
|          MethodReturnValuesAnalyzer |  1,088.242 us |  21.5275 us |  50.3197 us |  1,080.329 us |           - |           - |           - |             73728 B |
|              ObjectCreationAnalyzer |  1,077.396 us |  25.9302 us |  25.4669 us |  1,077.396 us |           - |           - |           - |             65536 B |
|                 ReturnValueAnalyzer |  2,382.574 us |  60.3600 us | 171.2312 us |  2,345.698 us |           - |           - |           - |             57344 B |
|              UsingStatementAnalyzer |  2,096.224 us |  41.0495 us |  60.1698 us |  2,082.067 us |           - |           - |           - |             42120 B |
|              IDISP001DisposeCreated |  1,033.455 us |  22.1805 us |  34.5324 us |  1,027.984 us |           - |           - |           - |             32768 B |
|           IDISP004DontIgnoreCreated |  8,745.638 us | 167.6867 us | 186.3833 us |  8,682.805 us |           - |           - |           - |            229376 B |
|          SemanticModelCacheAnalyzer |      9.880 us |   2.3544 us |   6.9420 us |      5.865 us |           - |           - |           - |                   - |
