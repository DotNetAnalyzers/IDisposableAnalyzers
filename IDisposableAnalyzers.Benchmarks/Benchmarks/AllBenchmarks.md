``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Xeon CPU E5-2637 v4 3.50GHz, 2 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


```
|                              Method |          Mean |      Error |       StdDev |        Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------ |--------------:|-----------:|-------------:|--------------:|------:|------:|------:|----------:|
|                    ArgumentAnalyzer |  3,196.285 us | 139.090 us |   385.418 us |  3,048.400 us |     - |     - |     - |  101680 B |
|                  AssignmentAnalyzer | 16,247.031 us | 385.044 us | 1,092.307 us | 15,858.600 us |     - |     - |     - |  739624 B |
|                 DisposeCallAnalyzer | 10,382.198 us | 214.173 us |   614.503 us | 10,093.600 us |     - |     - |     - |  313192 B |
|               DisposeMethodAnalyzer |    591.864 us |  23.995 us |    66.090 us |    584.800 us |     - |     - |     - |    7680 B |
| FieldAndPropertyDeclarationAnalyzer |  5,778.288 us | 240.944 us |   671.656 us |  5,557.050 us |     - |     - |     - |  146040 B |
|                   FinalizerAnalyzer |      9.507 us |   2.057 us |     5.801 us |      6.450 us |     - |     - |     - |     440 B |
|          MethodReturnValuesAnalyzer |  1,191.328 us |  62.658 us |   176.728 us |  1,128.750 us |     - |     - |     - |   39912 B |
|              ObjectCreationAnalyzer |  8,713.964 us | 314.430 us |   860.748 us |  8,521.500 us |     - |     - |     - |  399104 B |
|                 ReturnValueAnalyzer |  2,294.565 us | 140.719 us |   401.481 us |  2,131.700 us |     - |     - |     - |   60160 B |
|              UsingStatementAnalyzer |  6,809.184 us | 399.078 us | 1,151.431 us |  6,341.750 us |     - |     - |     - |  151936 B |
|              IDISP001DisposeCreated |  1,001.825 us |  59.631 us |   163.238 us |    937.600 us |     - |     - |     - |   26304 B |
|          SemanticModelCacheAnalyzer |     62.789 us |  11.949 us |    34.474 us |     47.650 us |     - |     - |     - |    1744 B |
