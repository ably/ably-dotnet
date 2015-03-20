using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ably
{
    internal interface IAblyHttpClient
    {
        AblyResponse Execute(AblyRequest request);
    }
}
