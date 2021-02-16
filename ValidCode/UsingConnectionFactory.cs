// ReSharper disable All
namespace ValidCode
{
    using System.Data.Entity.Infrastructure;

    public class UsingConnectionFactory
    {
        private readonly SqlConnectionFactory connectionFactory;

        public UsingConnectionFactory(SqlConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public void M()
        {
            this.connectionFactory.CreateConnection(string.Empty).Dispose();
            this.connectionFactory.CreateConnection(string.Empty)?.Dispose();
        }
    }
}
