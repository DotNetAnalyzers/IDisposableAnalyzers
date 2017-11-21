``` ini

BenchmarkDotNet=v0.10.10, OS=Windows 7 SP1 (6.1.7601.0)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410117 Hz, Resolution=293.2451 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
|                                         Method |         Mean |         Error |         StdDev |     Gen 0 |    Gen 1 |  Allocated |
|----------------------------------------------- |-------------:|--------------:|---------------:|----------:|---------:|-----------:|
|                         IDISP001DisposeCreated |   4,632.5 us |    104.041 us |    296.8350 us |   31.2500 |        - |   219138 B |
|                          IDISP002DisposeMember | 126,555.9 us |  2,521.317 us |  7,028.4247 us |  625.0000 |        - |  4163118 B |
|               IDISP003DisposeBeforeReassigning | 536,786.2 us | 10,675.946 us | 28,496.2618 us | 5937.5000 | 125.0000 | 37574774 B |
| IDISP004DontIgnoreReturnValueOfTypeIDisposable |  15,475.4 us |    308.961 us |    624.1166 us |  156.2500 |        - |  1096466 B |
|    IDISP005ReturntypeShouldIndicateIDisposable |   2,454.2 us |     48.907 us |     43.3545 us |   15.6250 |        - |   109697 B |
|                   IDISP006ImplementIDisposable | 109,559.7 us |  2,158.835 us |  2,730.2350 us |  625.0000 |        - |  4163118 B |
|                    IDISP007DontDisposeInjected |   2,334.9 us |     20.644 us |     17.2387 us |         - |        - |    21414 B |
|     IDISP008DontMixInjectedAndCreatedForMember | 111,660.2 us |    599.152 us |    467.7787 us |  625.0000 |        - |  4207673 B |
|                          IDISP009IsIDisposable |     381.9 us |      1.080 us |      0.8430 us |         - |        - |      836 B |
