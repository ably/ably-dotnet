using System;

namespace IO.Ably.PubSub
{
    /// <summary>
    /// Client options plumbing shared by the per-side PubSub packages, so that every factory door
    /// accepts the same three shapes of input. Compiled into both the IO.Ably.PubSub.Device and
    /// IO.Ably.PubSub.Server assemblies.
    /// </summary>
    internal static class PubSubOptions
    {
        /// <summary>
        /// Guards a caller-supplied options instance, so that a missing one is reported against the
        /// factory the caller actually used rather than deeper inside the core client.
        /// </summary>
        /// <param name="options"> The options the caller passed in. </param>
        /// <returns> The same options instance. </returns>
        internal static ClientOptions Required(ClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return options;
        }

        /// <summary>
        /// Builds a fresh <see cref="ClientOptions"/> configured by the supplied action. The core
        /// AblyRealtime has no such constructor overload, so the factories provide one uniformly.
        /// </summary>
        /// <param name="init"> Action that populates the new options. </param>
        /// <returns> The populated options. </returns>
        internal static ClientOptions Configure(Action<ClientOptions> init)
        {
            if (init == null)
            {
                throw new ArgumentNullException(nameof(init));
            }

            var options = new ClientOptions();
            init(options);

            return options;
        }
    }
}
