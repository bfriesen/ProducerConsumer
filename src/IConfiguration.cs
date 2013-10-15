using System;

namespace RandomSkunk.ProducerConsumer
{
    /// <summary>
    /// Provides values for the various configurable settings for the library.
    /// </summary>
    /// <typeparam name="T">
    /// The type of item to be produced and consumed by this instance of <see cref="Configuration{T}"/>.
    /// </typeparam>
    public interface IConfiguration<T>
    {
        /// <summary>
        /// Gets the <see cref="Action{T}"/> that will be executed by the consumer thread.
        /// </summary>
        Action<T> ConsumerAction { get; }

        /// <summary>
        /// Gets the <see cref="Action{Exception}"/> that will be executed if the consumer action throws an exception. If not set or set to null, the exception will be caught and ignored.
        /// </summary>
        Action<Exception> ErrorHandler { get; }

        /// <summary>
        /// Gets a value indicating whether to start the consumer thread immediately.
        /// </summary>
        bool StartImmediately { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="ProducerConsumer{T}.Enqueue"/> should add data items when <see cref="ProducerConsumer{T}.IsRunning"/> is false.
        /// </summary>
        bool EnqueueWhenStopped { get; }

        /// <summary>
        /// Gets a value indicating whether to call <see cref="ProducerConsumer{T}.Clear"/> when <see cref="ProducerConsumer{T}.IsRunning"/> is set to false.
        /// </summary>
        bool ClearQueueUponStop { get; }
    }
}
