using System.Reflection;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using IDisposableAnalyzers.Benchmarks;

[assembly: AssemblyTitle("IDisposableAnalyzers.Benchmarks")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("IDisposableAnalyzers.Benchmarks")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("dfbd789a-0f36-4691-8582-e3809e83a5ea")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: Config(typeof(MemoryDiagnoserConfig))]