``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0


```
 |                                         Method |         Mean |         Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
 |----------------------------------------------- |-------------:|--------------:|-------------:|----------:|---------:|-----------:|
 |                         IDISP001DisposeCreated | 514,152.0 us | 10,212.517 us | 22,841.76 us | 6812.5000 | 312.5000 | 43059898 B |
 |                          IDISP002DisposeMember | 159,134.1 us |  3,182.401 us |  6,642.86 us |  750.0000 |        - |  5151549 B |
 |               IDISP003DisposeBeforeReassigning | 537,771.3 us | 10,174.380 us |  9,019.32 us | 5500.0000 | 125.0000 | 34773210 B |
 | IDISP004DontIgnoreReturnValueOfTypeIDisposable |  12,098.6 us |    309.770 us |    913.36 us |   93.7500 |        - |   701445 B |
 |    IDISP005ReturntypeShouldIndicateIDisposable |   1,308.7 us |     29.785 us |     87.82 us |    1.9531 |        - |    22576 B |
 |                   IDISP006ImplementIDisposable | 154,670.3 us |  3,049.079 us |  7,363.87 us |  750.0000 |        - |  5151545 B |
 |                    IDISP007DontDisposeInjected |   5,353.6 us |    120.551 us |    355.45 us |   54.6875 |        - |   363011 B |
 |     IDISP008DontMixInjectedAndCreatedForMember | 148,303.5 us |  2,934.939 us |  6,442.26 us |  750.0000 |        - |  5179441 B |
 |                          IDISP009IsIDisposable |     318.6 us |      6.364 us |     17.42 us |         - |        - |      100 B |
