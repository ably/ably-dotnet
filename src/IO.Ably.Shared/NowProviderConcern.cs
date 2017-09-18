using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably.Shared
{
    /// <summary>
    /// Designed to be inherited by a class (or instanced and composed on to a class that implements INowProvider) that want to be concerned with providing a Now() implementation
    /// </summary>
    public class NowProviderConcern : INowProvider
    {
        internal INowProvider NowProvider { get; set; }
        protected NowProviderConcern(INowProvider nowProvider)
        {
            NowProvider = nowProvider;
        }

        public NowProviderConcern()
        {}

        public DateTimeOffset Now()
        {
            return NowProvider.Now();
        }
    }
}
