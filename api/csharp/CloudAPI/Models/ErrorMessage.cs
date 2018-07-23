using System;
using System.Collections.Generic;
using System.Text;

namespace CloudApi.Models
{
    /// <summary>
    ///   A basic error message with singlular instance and no accompanying metadata.
    /// </summary>
    /// 
    public class ErrorMessage
    {
        /// <summary>The message text.</summary>
        public readonly string Message;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ErrorMessage"/> class.
        /// </summary>
        /// 
        /// <param name="message">The message text.</param>
        /// 
        public ErrorMessage(string message)
        {
            this.Message = message;
        }
    }
}
