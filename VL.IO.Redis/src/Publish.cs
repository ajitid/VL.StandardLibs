﻿using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Model;

namespace VL.IO.Redis
{
    /// <summary>
    /// Publish a message on a specified Redis Channel. The Message will not saved in the database!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ProcessNode]
    public class Publish<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly ILogger _logger;

        private (RedisClient? client, string? redisChannel, IObservable<T>? value, SerializationFormat? format) _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Publish([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose() 
        {
            _subscription.Dispose();
        }

        public void Update(
            RedisClient? client, 
            string? redisChannel, 
            IObservable<T>? input, 
            Optional<SerializationFormat> serializationFormat = default)
        {
            var config = (client, redisChannel, input, format: serializationFormat.ToNullable());
            if (config == _config)
                return;

            _config = config;
            _subscription.Disposable = null;

            if (client is null || redisChannel is null || input is null)
                return;

            _subscription.Disposable = input.Subscribe(v =>
            {
                try
                {
                    var subscriber = client.GetSubscriber();
                    var channel = new RedisChannel(redisChannel, RedisChannel.PatternMode.Literal);
                    var value = client.Serialize(v, config.format);
                    subscriber.Publish(channel, value, CommandFlags.FireAndForget);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while publishing.");
                }
            });
        }
    }
}
