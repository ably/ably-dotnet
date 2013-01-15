using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ably
{
    public static class Guard
    {

    }

    public static class ExceptionExtensions
    {
    	public static void Throw(this Exception ex)
        {
            throw new AblyException("Check inner exception: " + ex.Message, ex);
        }
    }
}
