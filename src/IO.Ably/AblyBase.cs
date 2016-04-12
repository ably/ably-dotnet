using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably
{
    public class AblyBase
    {
        //TODO: MG Move to use the AblyHttpClient
        public static bool CanConnectToAbly()
        {
            WebRequest req = WebRequest.Create(Defaults.InternetCheckURL);
            WebResponse res = null;
            try
            {
                Func<Task<WebResponse>> fn = () => req.GetResponseAsync();
                res = Task.Run(fn).Result;
            }
            catch (Exception)
            {
                return false;
            }
            using (var resStream = res.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(resStream))
                {
                    return reader.ReadLine() == Defaults.InternetCheckOKMessage;
                }
            }
        }

        
    }
}