using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ably
{
    public interface IAblyHttpClient
    {
        AblyResponse Get(AblyRequest request);
        AblyResponse Delete(AblyResponse request);
        AblyResponse Post(AblyResponse request);
    }
}
