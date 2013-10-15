using System;
using System.Collections.Generic;
using System.Threading;

namespace RandomSkunk.ProducerConsumer
{
    /// <summary>
    /// A class to synchronize between producers and a consumer.
    /// </summary>
    /// <typeparam name="T">
    /// The type of item to be produced and consumed.
    /// </typeparam>
    public class ProducerConsumer<T>
    {
        /// <summary>
        /// The configuration for this instance of <see cref="ProducerConsumer{T}"/>.
        /// </summary>
        private readonly IConfiguration<T> _configuration;

        /// <summary>
        /// The <see cref="Queue{T}"/> that contains the data items.
        /// </summary>
        private readonly Queue<T> _queue = new Queue<T>();

        /// <summary>
        /// Synchronizes access to <see cref="_queue"/>.
        /// </summary>
        private readonly object _queueLocker = new object();

        /// <summary>
        /// Allows the consumer thread to block when no items are available in the <see cref="_queue"/>.
        /// </summary>
        private readonly AutoResetEvent _queueWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Prevents more than one thread from modifying <see cref="IsRunning"/> at a time.
        /// </summary>
        private readonly object _isRunningLocker = new object();

        /// <summary>
        /// Allows the consumer thread to block when <see cref="IsRunning"/> is false.
        /// </summary>
        private readonly AutoResetEvent _isRunningWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// Whether the consumer thread is processing data items.
        /// </summary>
        private volatile bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerConsumer{T}"/> class.
        /// </summary>
        /// <param name="configuration">
        /// The configuration for this instance of <see cref="ProducerConsumer{T}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        public ProducerConsumer(IConfiguration<T> configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _configuration = configuration;

            _isRunning = configuration.StartImmediately;

#if PORTABLE
            if (!ThreadPool.QueueUserWorkItem(ConsumeItems))
            {
                throw new InvalidOperationException("Unable to queue work item.");
            }
#else
            new Thread(ConsumeItems) { IsBackground = true }.Start();
#endif
        }

        /// <summary>
        /// Gets or sets a value indicating whether the consumer thread is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            set
            {
                // Allow only one thread at a time to modify IsRunning.
                lock (_isRunningLocker)
                {
                    if (value == _isRunning)
                    {
                        return;
                    }

                    if (value)
                    {
                        // Make sure queueWaitHandle is in a non-signalled state (so it will block) before signalling isRunningWaitHandle.
                        _queueWaitHandle.Reset();

                        // Also make sure to set isRunning to true before signalling isRunningWaitHandle.
                        _isRunning = true;

                        // Make sure to signal isRunningWaitHandle AFTER we are sure that queueWaitHandle is non-signalled and isRunning is true.
                        _isRunningWaitHandle.Set();
                    }
                    else
                    {
                        // Make sure isRunningWaitHandle is in a non-signalled state (so it will block) BEFORE setting isRunning to false or signalling queueWaitHandle.
                        _isRunningWaitHandle.Reset();

                        // Make sure to set isRunning to false AFTER we are sure isRunningWaitHandle is non-signalled (will block).
                        _isRunning = false;

                        // Make sure to signal queueWaitHandle AFTER we are sure that isRunningWaitHandle is non-signalled (will block), and isRunning is set to false.
                        _queueWaitHandle.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Start the consumer thread.
        /// </summary>
        public void Start()
        {
            IsRunning = true;
        }

        /// <summary>
        /// Stop the consumer thread.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Clear all data items from the queue.
        /// </summary>
        public void Clear()
        {
            lock (_queueLocker)
            {
                _queue.Clear();
            }
        }

        /// <summary>
        /// Enqueue a data item.
        /// </summary>
        /// <param name="item">
        /// The data item to enqueue.
        /// </param>
        public void Enqueue(T item)
        {
            lock (_queueLocker)
            {
                // If we're running, or we should queue items up when we're stopped...
                if (_isRunning || _configuration.EnqueueWhenStopped)
                {
                    // ...queue up the item...
                    _queue.Enqueue(item);

                    // ...and signal the consumer thread.
                    _queueWaitHandle.Set();
                }
            }
        }

        // ReSharper disable FunctionNeverReturns
        /// <summary>
        /// The consumer thread.
        /// </summary>
        /// <param name="state">
        /// Ignored, but required by ThreadPool.QueueUserWorkItem.
        /// </param>
        private void ConsumeItems(object state)
        {
            while (true)
            {
                if (_isRunning)
                {
                    T nextItem = default(T);

                    // Later on, we'll need to know whether there was an item in the queue.
                    bool doesItemExist;

                    lock (_queueLocker)
                    {
                        doesItemExist = _queue.Count > 0;
                        if (doesItemExist)
                        {
                            nextItem = _queue.Dequeue();
                        }
                    }

                    if (doesItemExist)
                    {
                        try
                        {
                            // If there was an item in the queue, process it...
                            _configuration.ConsumerAction(nextItem);
                        }
                        catch (Exception ex)
                        {
                            // ...but don't kill the thread if there was an exception...
                            
                            // ReSharper disable EmptyGeneralCatchClause
                            try
                            {
                                _configuration.ErrorHandler(ex);
                            }
                            catch
                            {
                                // ...and if the exception handler itself threw an exception, eat it and move on.
                            }
                            // ReSharper restore EmptyGeneralCatchClause
                        }
                    }
                    else
                    {
                        // ...otherwise, wait for the an item to be queued up.
                        _queueWaitHandle.WaitOne();
                    }
                }
                else
                {
                    if (_configuration.ClearQueueUponStop)
                    {
                        // We have just stopped, so clear the queue if we're configured to do so.
                        Clear();
                    }

                    // Wait to start up again.
                    _isRunningWaitHandle.WaitOne();
                }
            }
        }
        // ReSharper restore FunctionNeverReturns
    }
}