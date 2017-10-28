``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 7 SP1 (6.1.7601)
Processor=Intel Xeon CPU E5-2637 v4 3.50GHzIntel Xeon CPU E5-2637 v4 3.50GHz, ProcessorCount=16
Frequency=3410087 Hz, Resolution=293.2477 ns, Timer=TSC
  [Host]     : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0
  DefaultJob : .NET Framework 4.7 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.2116.0


```
 |                                         Method |         Mean |        Error |       StdDev |       Median |     Gen 0 |    Gen 1 |  Allocated |
 |----------------------------------------------- |-------------:|-------------:|-------------:|-------------:|----------:|---------:|-----------:|
 |                         IDISP001DisposeCreated | 574,421.6 us | 11,433.08 us | 32,433.73 us | 571,742.3 us | 7875.0000 | 329.1667 | 49735368 B |
 |                          IDISP002DisposeMember | 155,810.5 us |  3,930.96 us | 11,466.78 us | 154,574.0 us |  812.5000 |        - |  5253200 B |
 |               IDISP003DisposeBeforeReassigning | 559,614.9 us | 13,607.19 us | 40,121.12 us | 554,165.2 us | 5937.5000 | 187.5000 | 37519354 B |
 | IDISP004DontIgnoreReturnValueOfTypeIDisposable |   9,813.2 us |    199.45 us |    575.45 us |   9,781.1 us |   93.7500 |        - |   644358 B |
 |    IDISP005ReturntypeShouldIndicateIDisposable |   1,294.1 us |     26.71 us |     77.92 us |   1,290.3 us |         - |        - |    12544 B |
 |                   IDISP006ImplementIDisposable | 166,582.3 us |  7,739.26 us | 22,819.37 us | 153,537.8 us |  812.5000 |        - |  5253200 B |
 |                    IDISP007DontDisposeInjected |   5,414.9 us |    107.88 us |    298.93 us |   5,384.8 us |   54.6875 |        - |   396548 B |
 |     IDISP008DontMixInjectedAndCreatedForMember | 146,007.4 us |  2,900.42 us |  5,518.35 us | 145,215.0 us |  812.5000 |        - |  5294726 B |
 |                          IDISP009IsIDisposable |     463.7 us |     36.44 us |    107.46 us |     403.4 us |         - |        - |      436 B |
