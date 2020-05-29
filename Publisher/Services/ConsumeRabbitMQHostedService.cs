using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Publisher.Services
{
    public class ConsumeRabbitMQHostedService : BackgroundService  
    {
        public ConsumeRabbitMQHostedService([FromServices]RabbitMQConfigurations configurations, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            this._configuration = configurations;
            this._logger = loggerFactory.CreateLogger<ConsumeRabbitMQHostedService>();  
            InitRabbitMq();  
        }
        
        private readonly ILogger _logger;
        private readonly RabbitMQConfigurations _configuration;
        private IConnection _connection;  
        private IModel _channel;
        
        
        private void InitRabbitMq()
        {
            var factory = new ConnectionFactory()
            {
                HostName = this._configuration.HostName,
                Port = this._configuration.Port,
                UserName = this._configuration.UserName,
                Password = this._configuration.Password
            };
            
            _connection = factory.CreateConnection();
            
            _channel = _connection.CreateModel();  
  
            _channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);  
            _channel.QueueDeclare("demo.queue.log", false, false, false, null);  
            _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);  
            _channel.BasicQos(0, 1, false);  
  
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown; 
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)  
        {  
            stoppingToken.ThrowIfCancellationRequested();  
  
            var consumer = new EventingBasicConsumer(_channel);  
            consumer.Received += (ch, ea) =>  
            {  
                // received message 
                var body = ea.Body.Span;
                var content = System.Text.Encoding.UTF8.GetString(body);  
  
                // handle the received message  
                HandleMessage(content);  
                _channel.BasicAck(ea.DeliveryTag, false);  
            };  
  
            consumer.Shutdown += OnConsumerShutdown;  
            consumer.Registered += OnConsumerRegistered;  
            consumer.Unregistered += OnConsumerUnregistered;  
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;  
  
            _channel.BasicConsume("demo.queue.log", false, consumer);  
            return Task.CompletedTask;  
        }  
  
        private void HandleMessage(string content)  
        {  
            // we just print this message   
            _logger.LogInformation($"consumer received {content}");  
        }  
      
        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e)  {  }  
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) {  }  
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) {  }  
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)  {  }  
  
        public override void Dispose()  
        {  
            _channel.Close();  
            _connection.Close();  
            base.Dispose();  
        }
    }
   
}