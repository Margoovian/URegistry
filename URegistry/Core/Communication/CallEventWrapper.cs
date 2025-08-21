using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URegistry.Core.Communication
{
    /// <summary>
    /// Wrapper for communication between the plugin and the application
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct CallEventWrapper<T>
    {
        /// <summary>
        /// The function to call when the event is invoked, set by the application
        /// </summary>
        public Action<T, EventArgs> Func { get; init; }

        /// <summary>
        /// Used to invoke the event with the sender and arguments, used by the plugin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void Call(T sender, EventArgs args)
        {
            Func?.Invoke(sender, args);
        }
    }
}
