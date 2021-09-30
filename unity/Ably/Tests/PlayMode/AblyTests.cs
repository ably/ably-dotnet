using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
            Debug.Log("Connected to server 1");
            // Do stuff  
        });
        realtime.Connect();
        Thread.Sleep(2000);
        var channel = realtime.Channels.Get("testChannel");
        channel.Subscribe("lol",message =>
        {
            Debug.Log(message);
        });
        channel.Publish("lol", "my first message");
        Thread.Sleep(2000);
    }
}
