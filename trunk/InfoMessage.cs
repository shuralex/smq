using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMessageQueue
{
    /// <summary>
    /// an informative message that we fire off and forget. 
    /// Example: a logging message to send to another thread.
    /// </summary>
    public class InfoMessage:Message
    {
        public enum InfoType
        {
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// empty Info packet.
        /// </summary>
        public InfoMessage()
        {
            base.type = MessageType.Info;
        }

        /// <summary>
        /// construct and send with the data.
        /// </summary>
        /// <param name="args">Data to send</param>
        public InfoMessage(object[] args)
        {
            base.type = MessageType.Info;
            base.Args = args;
        }
    }
}
