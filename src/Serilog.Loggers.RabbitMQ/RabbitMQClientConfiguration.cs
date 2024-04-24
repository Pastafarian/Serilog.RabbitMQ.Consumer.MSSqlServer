// Copyright 2015 Serilog Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using RabbitMQ.Client;
using Serilog.Events;

namespace Serilog.Loggers.RabbitMQ
{
    /// <summary>
    /// Configuration class for RabbitMqClient
    /// </summary>
    public class RabbitMQConfiguration
    {
        public RabbitMQConfiguration()
        {
            LoggingExchangeName = "log-exchange";
            AuditExchangeName = "audit-exchange";
        }

        public List<string> Hostnames { get; set; } = [];
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string LoggingExchangeName { get; set; }
        public string AuditExchangeName { get; set; }
        public string ExchangeType { get; set; } = string.Empty;
        public string RouteKey { get; set; } = string.Empty;
        public int Port { get; set; }
        public string VHost { get; set; } = string.Empty;
        public IProtocol Protocol { get; set; }
        public ushort Heartbeat { get; set; }
        public SslOption SslOption { get; set; }
        public LogEventLevel LogEventLevel { get; set; }
    }

    /// <summary>
    /// Configuration class for RabbitMqClient
    /// </summary>
    public class RabbitMQClientConfiguration : RabbitMQConfiguration
    {
        /// <summary>
        /// The maximum number of events to include in a single batch.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        /// <summary>The time to wait between checking for event batches.</summary>
        public TimeSpan Period { get; set; }

        public bool AutoCreateExchange { get; set; }

        public RabbitMQClientConfiguration()
        {
            BatchPostingLimit = 5;
            Period = TimeSpan.FromSeconds(2);
            AutoCreateExchange = true;
        }
    }
}
