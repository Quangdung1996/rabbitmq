using Autofac;
using EventBus;
using EventBus.Abstractions;
using EventBus.Events;
using EventBus.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        private const string BROKER_NAME = "eshop_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "eshop_event_bus";
        private readonly int _retryCount;
        private IModel _consumerChannel;
        private string _queueName;

        public readonly AsyncRetryPolicy _retryPolicy;
        private readonly Serilog.ILogger _logger;

        public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
                                ILifetimeScope autofac,
                                IEventBusSubscriptionsManager subsManager,
                                 Serilog.ILogger logger,
                                string queueName = null,
                                int retryCount = 5)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _queueName = queueName;
            _consumerChannel = CreateConsumerChannel(logger);
            _autofac = autofac;
            _retryCount = retryCount;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;

            _retryPolicy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.Error("error");
                });

            _logger = logger;
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }

            _subsManager.Clear();
        }

        public async Task PublishAsync(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var eventName = @event.GetType().Name;

            _logger.Information("Creating RabbitMQ channel to publish event: {0} {1})", @event.Id, eventName);

            try
            {
                using (var channel = _persistentConnection.CreateModel())
                {
                    _logger.Information("Declaring RabbitMQ exchange to publish event: {0}", @event.Id);

                    channel.ExchangeDeclare(exchange: BROKER_NAME, ExchangeType.Direct);
                    channel.QueueBind(_queueName, BROKER_NAME, eventName);

                    var message = JsonConvert.SerializeObject(@event);
                    var body = Encoding.UTF8.GetBytes(message);

                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var properties = channel.CreateBasicProperties();
                        properties.DeliveryMode = 2; // persistent

                        _logger.Information("Publishing event to RabbitMQ: {0}", @event.Id);

                        channel.BasicPublish(exchange: BROKER_NAME, routingKey: eventName, mandatory: true,
                                             basicProperties: properties, body: body);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Could not publish event: {0} after  ({1})", @event.Id, ex.Message);
                throw;
            }
        }

        public async Task SubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);

            _logger.Information("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

            _subsManager.AddSubscription<T, TH>();
            StartBasicConsume();
        }

        public async Task SubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _logger.Information("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).GetGenericTypeName());

            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
            StartBasicConsume();
        }

        public async Task UnsubscribeAsync<T, TH>() where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();

            _logger.Information("Unsubscribing from event {EventName}", eventName);

            _subsManager.RemoveSubscription<T, TH>();
        }

        public async Task UnsubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using (var channel = _persistentConnection.CreateModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }

        private IModel CreateConsumerChannel(Serilog.ILogger logger)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            logger.Information("Creating RabbitMQ consumer channel");

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                    type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                logger.Warning($"Recreating RabbitMQ consumer channel {ea.Exception}");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel(logger);
                StartBasicConsume();
            };

            return channel;
        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);

                if (_subsManager.IsEmpty)
                {
                    _queueName = string.Empty;
                    _consumerChannel.Close();
                }
            }
        }

        private void StartBasicConsume()
        {
            _logger.Information("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                _logger.Error("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.Error($"----- ERROR Processing message {message}");
            }

            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.Information("Processing RabbitMQ event: {EventName}", eventName);

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                using (var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                {
                    var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                    foreach (var subscription in subscriptions)
                    {
                        if (subscription.IsDynamic)
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                            if (handler == null) continue;
                            dynamic eventData = JObject.Parse(message);

                            await Task.Yield();
                            await handler.HandleAsync(eventData);
                        }
                        else
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            if (handler == null) continue;
                            var eventType = _subsManager.GetEventTypeByName(eventName);
                            var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                            await Task.Yield();
                            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        }
                    }
                }
            }
            else
            {
                _logger.Error($"No subscription for RabbitMQ event: {eventName}");
            }
        }
    }
}