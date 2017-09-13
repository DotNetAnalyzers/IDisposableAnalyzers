``` ini

BenchmarkDotNet=v0.10.4, OS=Windows 10.0.14393
Processor=Intel Core i7-3667U CPU 2.00GHz (Ivy Bridge), ProcessorCount=4
Frequency=2435873 Hz, Resolution=410.5304 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0
  DefaultJob : Clr 4.0.30319.42000, 32bit LegacyJIT-v4.6.1637.0


```
 |                      Method |        Mean |      Error |     StdDev |      Gen 0 |     Gen 1 | Allocated |
 |---------------------------- |------------:|-----------:|-----------:|-----------:|----------:|----------:|
 | GetAnalyzerDiagnosticsAsync | 602.6861 ms | 10.8454 ms | 10.1448 ms | 10095.8333 | 1233.3333 |   0.04 GB |
