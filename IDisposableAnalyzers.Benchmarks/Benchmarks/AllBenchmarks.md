``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410126 Hz, Resolution=293.2443 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2114.0


```
 |                                         Method |         Mean |         Error |       StdDev |     Gen 0 |    Gen 1 |  Allocated |
 |----------------------------------------------- |-------------:|--------------:|-------------:|----------:|---------:|-----------:|
 |                         IDISP001DisposeCreated | 520,298.1 us | 10,407.559 us | 22,844.84 us | 6812.5000 | 250.0000 | 43083002 B |
 |                          IDISP002DisposeMember | 156,210.4 us |  3,102.335 us |  8,063.38 us |  812.5000 |        - |  5213267 B |
 |               IDISP003DisposeBeforeReassigning | 500,195.4 us |  9,808.254 us | 17,934.95 us | 5500.0000 | 125.0000 | 34766670 B |
 | IDISP004DontIgnoreReturnValueOfTypeIDisposable |  12,378.3 us |    339.989 us |  1,002.47 us |   93.7500 |        - |   698503 B |
 |    IDISP005ReturntypeShouldIndicateIDisposable |   1,314.3 us |     37.413 us |    109.14 us |    1.9531 |        - |    22128 B |
 |                   IDISP006ImplementIDisposable | 155,596.6 us |  3,088.417 us |  8,506.40 us |  812.5000 |        - |  5213267 B |
 |                    IDISP007DontDisposeInjected |   5,936.2 us |    215.664 us |    632.51 us |   54.6875 |        - |   361156 B |
 |     IDISP008DontMixInjectedAndCreatedForMember | 167,447.8 us |  3,701.021 us | 10,796.05 us |  812.5000 |        - |  5241435 B |
 |                          IDISP009IsIDisposable |     318.3 us |      9.550 us |     28.16 us |         - |        - |      100 B |
