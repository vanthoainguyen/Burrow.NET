﻿using System.Linq;
using Burrow.Internal;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Burrow.Tests.Internal.DurableConnectionTests
{
    public class DurableConnectionTestHelper
    {
        [TearDown]
        public void CleanUp()
        {
            ManagedConnectionFactory.SharedConnections.Values.ToList().ForEach(c =>
            {
                try
                {
                    c.Close(200, "Connection disposed by application");
                    c.Dispose();
                }
                catch
                {
                }
            });
            ManagedConnectionFactory.SharedConnections.Clear();
            
        }

        public static ConnectionFactory CreateMockConnectionFactory<T>(string virtualHost, AmqpTcpEndpoint endpoint = null) where T : ConnectionFactory
        {
            IConnection connection;
            return CreateMockConnectionFactory<T>(virtualHost, out connection, endpoint);
        }

        public static ConnectionFactory CreateMockConnectionFactory<T>(string virtualHost, out IConnection connection, AmqpTcpEndpoint endpoint = null) where T : ConnectionFactory
        {
            var conn = Substitute.For<IConnection>();

            var connectionFactory = Substitute.For<T>();
            connectionFactory.VirtualHost = virtualHost;
            if (typeof(T) == typeof(ManagedConnectionFactory))
            {
                (connectionFactory as ManagedConnectionFactory).EstablishConnection().Returns(conn);
            }
            else
            {
                connectionFactory.CreateConnection().Returns(conn);
            }
            if (endpoint != null)
            {
                connectionFactory.Endpoint = endpoint;
            }
            conn.Endpoint.Returns(connectionFactory.Endpoint);
            conn.IsOpen.Returns(true);
            connection = conn;

            return connectionFactory;
        }
    }
}
