using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably
{
    public interface INowProvider
    {
        DateTimeOffset Now();
    }
}
