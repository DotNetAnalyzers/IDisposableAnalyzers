namespace IDisposableAnalyzers.Tests.Web.IDISP004DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public class ExpectWarning
    {
        private const string BaseCode = @"
namespace N
{
    using System;
    public interface IDbConnection:IDisposable{}
    public class Connection: IDbConnection{public void Dispose(){}}
InterfaceOrImplementationHere

    public static class CheckIDisposableFromFactory
    {
        private static readonly IDbConnectionFactory _dbConnectionFactory = null!;
        public static void M1()
        {
            CallTarget.TestMethod(_dbConnectionFactory.GetConnection());
        }
    }
    public static class CallTarget
    {
        public static void TestMethod(IDbConnection connection)
        {
        }
    }
}";

        [Test]
        public void IDisposableReturnedFromImplementedFactory()
        {
            const string classDefinition = @"
    public class IDbConnectionFactory
    {
        public IDbConnection GetConnection()
        {
            return new Connection();
        }
    }";
            var code = BaseCode.Replace("InterfaceOrImplementationHere", classDefinition, StringComparison.OrdinalIgnoreCase);

            RoslynAssert.Diagnostics(new CreationAnalyzer(), ExpectedDiagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated), code);
        }

        [Test]
        public void IDisposableFromFactoryInterface()
        {
            const string interfaceDefinition = @"
    public interface IDbConnectionFactory
    {
        IDbConnection GetConnection();
    }";
            var code = BaseCode.Replace("InterfaceOrImplementationHere", interfaceDefinition, StringComparison.OrdinalIgnoreCase);

            RoslynAssert.Diagnostics(new CreationAnalyzer(), ExpectedDiagnostic.Create(Descriptors.IDISP004DoNotIgnoreCreated), code);
        }
    }
}
