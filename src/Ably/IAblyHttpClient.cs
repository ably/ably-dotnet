using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ably
{
    public interface IAblyHttpClient
    {
        AblyResponse Execute(AblyRequest request);
    }
}
