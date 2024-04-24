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

using Serilog.Loggers.RabbitMQ;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.RabbitMQ
{
    /// <summary>
    /// Configuration class for RabbitMqClient
    /// </summary>
    public class RabbitMqClientConsumerConfiguration : RabbitMQClientConfiguration
    {
        public RabbitMqClientConsumerConfiguration()
        {
            LoggingQueueName = "log-queue";
            AuditQueueName = "audit-queue";
        }

        /// <summary>
        /// Name of the RabbitMq queue for log events
        /// </summary>
        public string LoggingQueueName { get; set; }

        /// <summary>
        /// Name of the RabbitMq queue for audit events
        /// </summary>
        public string AuditQueueName { get; set; }
    }
}
