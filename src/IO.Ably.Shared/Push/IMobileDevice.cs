using System.Collections.Generic;

namespace IO.Ably.Push
{
    /// <summary>
    /// Interface for communicating with a mobile device supporting pushing notifications
    /// </summary>
    public interface IMobileDevice
    {
        /// <summary>
        /// Trigger Intent(Android) or ... (Apple)
        /// </summary>
        /// <param name="name">name of the intent. It will be prepended with io.ably.broadcast..</param>
        /// <param name="extraParameters">extra parameters set for the intent.</param>
        void SendIntent(string name, Dictionary<string, object> extraParameters);
    }
}