﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{
    public class Transaction<T> : IDisposable
    {
        readonly IDisposable disposable = null;
        readonly RedisBinding binding;
        private bool init = false;

        public Transaction(
            RedisBinding binding,
            Func<ITransaction, RedisKey, Task<RedisValue>> RedisChangedCommand,
            Func<ITransaction, KeyValuePair<RedisKey, RedisValue>, Task<bool>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Func<T, RedisValue> serialize,
            Func<bool, bool> DeserializeSet,
            Func<RedisValue, T> DeserializeGet)
        {
            this.binding = binding;

            this.init = binding.Initialisation == Initialisation.Redis;

            var onRedisChangeNotificationOrFirstFrame = OnRedisChangeNotificationOrFirstFrame(RedisChangedCommand);

            var onChannelChange = SerializeSetAndPushChanges(onRedisChangeNotificationOrFirstFrame, serialize, ChannelChangedCommand, pushChanges);

            var deserializeResult = Deserialize(DeserializeSet, DeserializeGet);

            disposable = onChannelChange.CombineLatest(deserializeResult, (c, d) => d).Subscribe();
        }

        private IObservable<RedisCommandQueue> OnRedisChangeNotificationOrFirstFrame(
            Func<ITransaction, RedisKey, Task<RedisValue>> RedisChangedCommand)
        {
            return binding.AfterFrame
                .Select((queue) =>
                {
                    if (queue.Transaction != null)
                    {
                        if
                        (
                            (binding.BindingType == RedisBindingType.Receive || binding.BindingType == RedisBindingType.SendAndReceive) &&
                            (
                                (queue.ReceivedChanges.Contains(binding.Key) && queue.ReceivedChanges.Count > 0) || init
                            )
                            ||
                            binding.BindingType == RedisBindingType.AlwaysReceive
                        )
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    RedisChangedCommand(tran, binding.Key).ContinueWith(t => new KeyValuePair<Guid, object>(binding.getID, (object)t.Result)),
                                    Enumerable.Empty<RedisKey>()
                                )
                            );
                        }
                    }

                    return queue;
                }
            );
        }

        private IObservable<ValueTuple<RedisCommandQueue, T>> SerializeSetAndPushChanges(
            IObservable<RedisCommandQueue> queue,
            Func<T, RedisValue> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, RedisValue>, Task<bool>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges)
        {
            return ReactiveExtensions.
                WithLatestWhenNew(binding.channel.ChannelOfObject, queue, (c, q) =>
                {
                    return ValueTuple.Create(q, (T)c);
                }).
                Select((input) =>
                {
                    var queue = input.Item1;

                    var value = input.Item2;

                    if (queue.Transaction != null && binding.channel.LatestAuthor != "RedisOther")
                    {
                        if (binding.BindingType == RedisBindingType.Send || binding.BindingType == RedisBindingType.SendAndReceive)
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    ChannelChangedCommand(tran, KeyValuePair.Create(binding.Key, serialize(value))).ContinueWith(t => new KeyValuePair<Guid, object>(binding.setID, (object)t.Result)),
                                    pushChanges.HasValue ? pushChanges.Value(binding.Key) : Enumerable.Empty<RedisKey>()
                                )
                            );
                        }
                    }
                    return input;
                }
            );
        }

        private IObservable<ValueTuple<bool, bool, bool>> Deserialize(
            Func<bool, bool> DeserializeSet,
            Func<RedisValue, T> DeserializeGet)
        {
            var beforeFrameResult = binding.BeforFrame
                .Select(t =>
                {
                    var dict = t;

                    bool OnSuccessfulWrite = false;
                    bool OnSuccessfulRead = false;
                    T result = default(T);

                    if (dict != null)
                    {
                        if (binding.CollisionHandling == CollisionHandling.RedisWins)
                        {
                            if (dict.TryGetValue(binding.getID, out var getValue))
                            {
                                if (!((RedisValue)getValue).IsNull)
                                {
                                    result = DeserializeGet((RedisValue)getValue);
                                    binding.channel.SetObjectAndAuthor(result, "RedisOther");
                                    OnSuccessfulRead = true;
                                    if (init)
                                    {
                                        init = false;
                                    }
                                }
                            }
                            if (dict.TryGetValue(binding.setID, out var setValue))
                            {
                                OnSuccessfulWrite = DeserializeSet((bool)setValue);
                            }
                        }
                        else
                        {
                            if (dict.TryGetValue(binding.setID, out var setValue))
                            {
                                OnSuccessfulWrite = DeserializeSet((bool)setValue);
                            }
                            if (dict.TryGetValue(binding.getID, out var getValue))
                            {
                                if (!((RedisValue)getValue).IsNull)
                                {
                                    result = DeserializeGet((RedisValue)getValue);
                                    binding.channel.SetObjectAndAuthor(result, "RedisOther");
                                    OnSuccessfulRead = true;
                                    if (init)
                                    {
                                        init = false;
                                    }
                                }
                            }
                        }
                    }


                    return ValueTuple.Create(OnSuccessfulWrite, OnSuccessfulRead, false, result);
                })
                .Publish()
                .RefCount();

            var nextFrameChannelUpdates = binding.channel.ChannelOfObject
                .Where(
                (value) =>
                {
                    return binding.channel.LatestAuthor != "RedisOther" && binding.CollisionHandling == CollisionHandling.RedisWins;
                })
                .WithLatestFrom(beforeFrameResult,
                (value, resultTuple) =>
                {
                    bool OnSuccessfulRead = false;
                    bool OnRedisOverWrite = false;

                    if (resultTuple.Item2)
                    {
                        binding.channel.SetObjectAndAuthor(resultTuple.Item4, "RedisOther");
                        OnSuccessfulRead = true;
                        OnRedisOverWrite = true;
                    }

                    return ValueTuple.Create(false, OnSuccessfulRead, OnRedisOverWrite, default(T));
                })
                .Where(T => T.Item3);

            return beforeFrameResult
                    .Merge(nextFrameChannelUpdates)
                    .Select(t =>
                    {
                        var result = binding.channel.Components.OfType<RedisResult>().FirstOrDefault();
                        if (result != null)
                        {
                            result.OnSuccessfulWrite = t.Item1;
                            result.OnSuccessfulRead = t.Item2;
                            result.OnRedisOverWrite = t.Item3;
                        }
                        return ValueTuple.Create(t.Item1, t.Item2, t.Item3);
                    });
        }

        public void Dispose()
        {
            disposable?.Dispose();
        }
    }
}
