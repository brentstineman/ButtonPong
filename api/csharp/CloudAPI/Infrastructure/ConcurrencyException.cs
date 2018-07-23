using System;
using System.Runtime.Serialization;

namespace CloudApi.Infrastructure
{
    /// <summary>
    ///   Represents an exception for the scenario when a needed dependency is missing.
    /// </summary>
    /// 
    /// <seealso cref="System.Exception" />
    /// 
    [Serializable]
    public class ConcurrencyException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// 
        public ConcurrencyException() : base() 
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// 
        public ConcurrencyException(string message) : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// 
        /// <param name="inner">The source exception that caused this exception scenario.</param>
        /// 
        public ConcurrencyException(Exception inner) : base(String.Empty, inner) 
        { 
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The source exception that caused this exception scenario.</param>
        /// 
        public ConcurrencyException(string    message, 
                                    Exception inner) : base(message, inner) 
        { 
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// 
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// 
        protected ConcurrencyException(SerializationInfo info,
                                       StreamingContext context) 
        { 
        }
    }
}
