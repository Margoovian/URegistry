using Microsoft.Extensions.Logging;
using URegistry.Core;

namespace URegistry.Interfaces
{
    public interface IPlugin
    {

        /// <summary>
        /// On plugin mount, this is the initial setup of the plugin - put any dependancies that may fail inside here so the fail message can bubble up and be caught
        /// </summary>
        /// <returns>False if mounting process is failed</returns>
        public bool OnMount();

        /// <summary>
        /// On plugin unmount, this is the last setup of the plugin being removed - put any dependancie shutdown code that may fail inside here so the fail message can bubble up and be caught
        /// </summary>
        /// <returns>False if unmounting process is failed</returns>
        public bool OnUnmount();

        /// <summary>
        /// Internal setup of the plugin, called after OnMount
        /// </summary>
        public void OnInitialized();

        /// <summary>
        /// Internal shutdown of the plugin, called before Unmount
        /// </summary>
        public void OnDeinitialized();

        /// <summary>
        /// Used for custom verification of the plugin, such as checking if all dependencies are met, etc.
        /// </summary>
        /// <returns>True if verification was successful</returns>
        public bool Verify() { return true; }

    }
}
