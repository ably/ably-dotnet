using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably
{
    internal interface ILoggerConcern
    {
        ILogger Logger { get; set; }
    }
}
