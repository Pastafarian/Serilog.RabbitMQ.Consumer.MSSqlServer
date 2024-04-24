using RabbitMQ.Client;
using Serilog.Loggers.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ
{
    public interface IRabbitConnectionFactory : IDisposable
    {
        Task<IConnection> GetConnectionAsync();
        void Close();
    }

    /// <summary>
    /// RabbitMqClient - this class is the engine that lets you send messages to RabbitMq
    /// </summary>
    public class RabbitConnectionFactory : IDisposable, IRabbitConnectionFactory
    {
        // synchronization lock
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        private readonly CancellationTokenSource _closeTokenSource = new();
        private readonly CancellationToken _closeToken;

        // configuration member
        private readonly RabbitMQClientConfiguration _config;

        // endpoint members
        private readonly IConnectionFactory _connectionFactory;
        private volatile IConnection? _connection;

        /// <summary>
        /// Constructor for RabbitMqClient
        /// </summary>
        /// <param name="configuration">mandatory</param>
        public RabbitConnectionFactory(RabbitMqClientConsumerConfiguration configuration)
        {
            _closeToken = _closeTokenSource.Token;

            // load configuration
            _config = configuration;
            // initialize
            _connectionFactory = GetConnectionFactory();
        }

        /// <summary>
        /// Configures a new ConnectionFactory, and returns it
        /// </summary>
        /// <returns></returns>
        private IConnectionFactory GetConnectionFactory()
        {
            // prepare connection factory
            var connectionFactory = new ConnectionFactory
            {
                UserName = _config.Username,
                Password = _config.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(2),
                ClientProvidedName = "serilog.sinks.rabbitmq consumer",
                DispatchConsumersAsync = true,
                ConsumerDispatchConcurrency = 1
            };

            if (_config.SslOption != null)
            {
                connectionFactory.Ssl.Version = _config.SslOption.Version;
                connectionFactory.Ssl.CertPath = _config.SslOption.CertPath;
                connectionFactory.Ssl.ServerName = _config.SslOption.ServerName;
                connectionFactory.Ssl.Enabled = _config.SslOption.Enabled;
                connectionFactory.Ssl.AcceptablePolicyErrors = _config.SslOption.AcceptablePolicyErrors;
            }
            // setup heartbeat if needed
            if (_config.Heartbeat > 0)
                connectionFactory.RequestedHeartbeat = TimeSpan.FromMilliseconds(_config.Heartbeat);

            // only set, if has value, otherwise leave default
            if (_config.Port > 0) connectionFactory.Port = _config.Port;
            if (!string.IsNullOrEmpty(_config.VHost)) connectionFactory.VirtualHost = _config.VHost;

            // return factory
            return connectionFactory;
        }

        /// <summary>

        public void Close()
        {
            IList<Exception> exceptions = new List<Exception>();
            try
            {
                _closeTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                _connectionLock.Wait(10);
                _connection?.Close();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _closeTokenSource.Dispose();
            _connectionLock.Dispose();
            _connection?.Dispose();
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                await _connectionLock.WaitAsync(_closeToken);
                try
                {
                    if (_connection == null)
                    {
                        _connection = _config.Hostnames.Count == 0
                            ? _connectionFactory.CreateConnection()
                            : _connectionFactory.CreateConnection(_config.Hostnames);
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }

            return _connection;
        }
    }
}
