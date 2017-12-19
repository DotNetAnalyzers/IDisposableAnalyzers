``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410097 Hz, Resolution=293.2468 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2117.0


```
|                                         Method |         Mean |          Error |         StdDev |       Median |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|---------------:|---------------:|-------------:|----------:|---------:|-----------:|
|                          DisposeMethodAnalyzer |     641.0 us |      0.7889 us |      0.7748 us |     641.0 us |         - |        - |      444 B |
|                       FieldDeclarationAnalyzer | 116,721.9 us |  2,329.0567 us |  3,760.9924 us | 116,851.6 us |  625.0000 |        - |  4056894 B |
|                         IDISP001DisposeCreated |   4,929.3 us |    114.9594 us |    338.9603 us |   4,948.2 us |   31.2500 |        - |   224067 B |
|               IDISP003DisposeBeforeReassigning | 538,237.7 us | 10,752.3363 us | 19,112.2669 us | 534,841.8 us | 6187.5000 | 187.5000 | 39160724 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  16,670.7 us |    378.7982 us |  1,110.9496 us |  16,323.8 us |  171.8750 |        - |  1121677 B |
|    IDISP005ReturntypeShouldIndicateIDisposable |   2,851.7 us |     56.9797 us |    155.0176 us |   2,818.0 us |   15.6250 |        - |   109697 B |
|                   IDISP006ImplementIDisposable | 118,587.2 us |  2,353.3075 us |  4,699.8093 us | 119,081.0 us |  625.0000 |        - |  4244268 B |
|                    IDISP007DontDisposeInjected |   2,459.0 us |     50.3935 us |    147.0003 us |   2,447.6 us |         - |        - |    21416 B |
|     IDISP008DontMixInjectedAndCreatedForMember | 124,043.3 us |  2,467.1773 us |  2,533.6092 us | 123,493.0 us |  625.0000 |        - |  4288826 B |
|                          IDISP009IsIDisposable |     406.5 us |      8.0468 us |     17.1485 us |     403.9 us |         - |        - |      836 B |
|                    PropertyDeclarationAnalyzer |   4,540.4 us |    105.9133 us |    312.2878 us |   4,488.7 us |   23.4375 |        - |   187841 B |
