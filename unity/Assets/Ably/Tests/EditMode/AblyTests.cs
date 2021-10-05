using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AblyTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void SampleTestsSimplePasses()
    {
        // Use the Assert class to test conditions
        Assert.True(true);
        var options = new ClientOptions();
        options.Key = "jy3uew.oZJBOA:L7FBaCQTrr9kgmmP";
        // this will disable the library trying to subscribe to network state notifications
        options.AutomaticNetworkStateMonitoring = false;

        var realtime = new AblyRealtime(options);
        realtime.Connection.On(ConnectionEvent.Connected, args =>
        {
            Console.WriteLine("Connected to server 1");
            // Do stuff  
        });
        realtime.Connect();
        Thread.Sleep(2000);
    }

    //// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    //// `yield return null;` to skip a frame.
    //[UnityTest]
    //public IEnumerator SampleTestsWithEnumeratorPasses()
    //{
    //    // Use the Assert class to test conditions.
    //    // Use yield to skip a frame.
    //    yield return null;
    //}
}
