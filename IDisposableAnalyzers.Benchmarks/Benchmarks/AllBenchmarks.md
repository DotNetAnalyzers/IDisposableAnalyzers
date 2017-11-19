``` ini

BenchmarkDotNet=v0.10.10, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
|                                         Method |         Mean |         Error |       StdDev |       Median |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|--------------:|-------------:|-------------:|----------:|---------:|-----------:|
|                         IDISP001DisposeCreated |   4,266.9 us |     97.162 us |    284.96 us |   4,172.3 us |   31.2500 |        - |   219525 B |
|                          IDISP002DisposeMember | 140,329.9 us |  2,796.453 us |  7,317.83 us | 139,477.7 us |  750.0000 |        - |  5158990 B |
|               IDISP003DisposeBeforeReassigning | 524,155.6 us | 10,280.513 us | 17,733.38 us | 526,572.6 us | 6000.0000 | 187.5000 | 38094153 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  15,600.3 us |    458.148 us |  1,350.86 us |  15,340.8 us |  125.0000 |        - |   994056 B |
|    IDISP005ReturntypeShouldIndicateIDisposable |   2,569.8 us |     58.213 us |    167.02 us |   2,538.5 us |   15.6250 |        - |   109569 B |
|                   IDISP006ImplementIDisposable | 148,164.0 us |  2,955.874 us |  7,250.81 us | 147,995.7 us |  750.0000 |        - |  5159007 B |
|                    IDISP007DontDisposeInjected |   2,504.7 us |     54.521 us |    157.31 us |   2,466.7 us |         - |        - |    21414 B |
|     IDISP008DontMixInjectedAndCreatedForMember | 146,173.0 us |  2,909.078 us |  8,061.04 us | 146,327.6 us |  750.0000 |        - |  5203781 B |
|                          IDISP009IsIDisposable |     411.1 us |      8.182 us |     19.45 us |     413.2 us |         - |        - |      836 B |
