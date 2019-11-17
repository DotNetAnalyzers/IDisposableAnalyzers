``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


```
|                              Method |          Mean |      Error |       StdDev |        Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------ |--------------:|-----------:|-------------:|--------------:|------:|------:|------:|----------:|
|                    ArgumentAnalyzer |  3,917.171 us | 430.473 us | 1,269.259 us |  3,383.750 us |     - |     - |     - |  101608 B |
|                  AssignmentAnalyzer | 17,485.028 us | 731.341 us | 2,062.760 us | 16,530.300 us |     - |     - |     - |  738688 B |
|                 DisposeCallAnalyzer | 10,224.764 us | 325.311 us |   879.497 us |  9,864.200 us |     - |     - |     - |  306152 B |
|               DisposeMethodAnalyzer |    944.799 us |  67.995 us |   192.892 us |    885.100 us |     - |     - |     - |   14984 B |
| FieldAndPropertyDeclarationAnalyzer |  5,548.094 us | 276.008 us |   796.347 us |  5,411.150 us |     - |     - |     - |  144432 B |
|                   FinalizerAnalyzer |      9.589 us |   1.291 us |     3.577 us |      8.700 us |     - |     - |     - |     440 B |
|          MethodReturnValuesAnalyzer |  1,110.909 us |  78.970 us |   221.442 us |  1,015.200 us |     - |     - |     - |   38488 B |
|              ObjectCreationAnalyzer |  7,787.076 us | 590.061 us | 1,683.477 us |  7,073.200 us |     - |     - |     - |  348640 B |
|                 ReturnValueAnalyzer |  2,637.545 us | 303.032 us |   893.496 us |  2,420.650 us |     - |     - |     - |   58560 B |
|              UsingStatementAnalyzer |  7,944.105 us | 646.676 us | 1,906.740 us |  7,826.950 us |     - |     - |     - |  146208 B |
|              IDISP001DisposeCreated |    932.036 us |  89.760 us |   257.538 us |    805.800 us |     - |     - |     - |   24648 B |
|          SemanticModelCacheAnalyzer |     65.754 us |  11.202 us |    33.029 us |     46.900 us |     - |     - |     - |    1760 B |
