``` ini

BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
Frequency=3410073 Hz, Resolution=293.2489 ns, Timer=TSC
  [Host]     : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3132.0
  DefaultJob : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.3132.0


```
|                              Method |          Mean |         Error |       StdDev |        Median | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------ |--------------:|--------------:|-------------:|--------------:|------------:|------------:|------------:|--------------------:|
|              IDISP001DisposeCreated |   3,038.08 us |    59.1007 us |    55.283 us |   3,019.00 us |           - |           - |           - |             81920 B |
|           IDISP004DontIgnoreCreated |  78,713.84 us |   299.4858 us |   265.486 us |  78,668.55 us |           - |           - |           - |           2056240 B |
|                    ArgumentAnalyzer | 123,807.79 us | 1,253.7476 us | 1,172.756 us | 123,611.14 us |           - |           - |           - |           4186112 B |
|                  AssignmentAnalyzer |  81,125.72 us | 1,119.0097 us |   991.973 us |  81,163.95 us |           - |           - |           - |           2564096 B |
|                 DisposeCallAnalyzer |  13,649.18 us |   233.0835 us |   206.622 us |  13,609.53 us |           - |           - |           - |                   - |
|               DisposeMethodAnalyzer |   1,821.08 us |    29.6956 us |    26.324 us |   1,818.88 us |           - |           - |           - |                   - |
| FieldAndPropertyDeclarationAnalyzer |  24,080.33 us |   308.9158 us |   288.960 us |  24,125.00 us |           - |           - |           - |            573440 B |
|          MethodReturnValuesAnalyzer |   2,508.68 us |    40.5389 us |    35.937 us |   2,501.41 us |           - |           - |           - |            196608 B |
|              ObjectCreationAnalyzer |   3,086.03 us |    60.0494 us |    86.121 us |   3,065.77 us |           - |           - |           - |            221184 B |
|                 ReturnValueAnalyzer |  29,136.22 us |   551.2803 us |   541.431 us |  29,000.70 us |           - |           - |           - |            909312 B |
|              UsingStatementAnalyzer |     604.69 us |    11.9581 us |    25.996 us |     594.42 us |           - |           - |           - |                   - |
|          SemanticModelCacheAnalyzer |      10.98 us |     0.5286 us |     1.345 us |      10.85 us |           - |           - |           - |                   - |
