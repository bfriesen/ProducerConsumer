using System;

namespace RandomSkunk.ProducerConsumer
{
    /// <summary>
    /// Contains the various configurable settings for the library.
    /// </summary>
    /// <typeparam name="T">The type of item to be produced and consumed by this instance of <see cref="Configuration{T}"/>.</typeparam>
    public class Configuration<T> : IConfiguration<T>
    {
        /// <summary>
        /// The <see cref="Action{T}"/> that will be executed by the consumer thread.
        /// </summary>
        private readonly Action<T> _consumerAction;

        /// <summary>
        /// The <see cref="Action{Exception}"/> that is executed if the consumer action throws an exception.
        /// </summary>
        private Action<Exception> _errorHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration{T}"/> class.
        /// </summary>
        /// <param name="consumerAction">The <see cref="Action{T}"/> that will be executed by the consumer thread.</param>
        public Configuration(Action<T> consumerAction)
        {
            if (consumerAction == null)
            {
                throw new ArgumentNullException("consumerAction");
            }

            _consumerAction = consumerAction;

            ErrorHandler = null;
            EnqueueWhenStopped = true;
            ClearQueueUponStop = false;
            StartImmediately = true;
        }

        /// <summary>
        /// Gets the <see cref="Action{T}"/> that will be executed by the consumer thread.
        /// </summary>
        public Action<T> ConsumerAction
        {
            get { return _consumerAction; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Action{Exception}"/> that will be executed if the consumer action throws an exception. If not set or set to null, the exception will be caught and ignored.
        /// </summary>
        public Action<Exception> ErrorHandler
        {
            get
            {
                return _errorHandler;
            }
            set
            {
                _errorHandler =
                    value == null
                        ? _ => {}
                        : value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to start the consumer thread immediately.
        /// </summary>
        public bool StartImmediately { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ProducerConsumer{T}.Enqueue"/> should add data items when <see cref="IsRunning"/> is false.
        /// </summary>
        public bool EnqueueWhenStopped { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to call <see cref="Clear"/> when <see cref="IsRunning"/> is set to false.
        /// </summary>
        public bool ClearQueueUponStop { get; set; }
    }
}
