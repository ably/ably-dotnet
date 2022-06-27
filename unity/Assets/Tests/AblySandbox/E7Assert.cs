using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Assets.Tests.AblySandbox
{
    public class E7Assert
    {
        public static async Task<T> ThrowsAsync<T>(Task asyncMethod) where T : Exception
        {
            return await ThrowsAsync<T>(asyncMethod, "");
        }

        public static async Task<T> ThrowsAsync<T>(Task asyncMethod, string message) where T : Exception
        {
            try
            {
                await asyncMethod; //Should throw..
            }
            catch (T e)
            {
                //Ok! Swallow the exception.
                return e;
            }
            catch (Exception e)
            {
                if (message != "")
                {
                    Assert.That(e, Is.TypeOf<T>(), message + " " + e.ToString()); //of course this fail because it goes through the first catch..
                }
                else
                {
                    Assert.That(e, Is.TypeOf<T>(), e.ToString());
                }
                return (T)e; //probably unreachable
            }
            Assert.Fail("Expected an exception of type " + typeof(T).FullName + " but no exception was thrown.");
            return null;
        }
    }
}
