﻿using System;
using Xunit;

namespace IO.Ably.Tests
{
    internal static class TestHelpers
    {
        public static void AssertContainsParameter(this AblyRequest request, string key, string value)
        {
            Assert.True(request.QueryParameters.ContainsKey(key),
                String.Format("Header '{0}' doesn't exist in request", key));
            Assert.Equal(value, request.QueryParameters[key]);
        }
    }
}
