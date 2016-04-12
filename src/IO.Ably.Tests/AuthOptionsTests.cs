using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IO.Ably.Tests
{
    public class AuthOptionsMergeTests
    {
        public AuthOptions GetBlankOptions()
        {
            return new AuthOptions();
        }

        public AuthOptions GetCompleteOptions()
        {
            return new AuthOptions()
            {
                AuthHeaders = new Dictionary<string, string> { {"Test", "Test"} },
                AuthParams = new Dictionary<string, string> { {"Test", "Test"} },
                Token = "Token",
                AuthUrl = "http://www.google.com",
                Key = "key",
                QueryTime = true,
                AuthCallback = param => null,
                UseTokenAuth = true
            };
        }


        [Fact]
        public void Merge_WithOptionsNotSet_OverwritesThem()
        {
            AuthOptions complete = GetCompleteOptions();
            var blankOptions = GetBlankOptions();
            blankOptions.Merge(complete);

            Assert.Equal(blankOptions.AuthHeaders, complete.AuthHeaders);
            Assert.Equal(blankOptions.AuthParams, complete.AuthParams);
            Assert.Equal(blankOptions.AuthParams, complete.AuthParams);
            Assert.Equal(blankOptions.AuthUrl, complete.AuthUrl);
            Assert.Equal(blankOptions.AuthCallback, complete.AuthCallback);
            Assert.Equal(blankOptions.QueryTime, complete.QueryTime);
            Assert.Equal(blankOptions.UseTokenAuth, complete.UseTokenAuth);
        }


        [Fact]
        public void Merge_WithOptionsNotSet_DoesNotOverwriteKey()
        {
            AuthOptions complete = GetCompleteOptions();
            var blankOptions = GetBlankOptions();
            complete.Merge(blankOptions);

            Assert.NotEqual(blankOptions.Key, complete.Key);
        }

        [Fact]
        public void Merge_WithCompleteOptions_DoesNotOverwriteAnything()
        {
            AuthOptions complete = GetCompleteOptions();
            var otherComplete = new AuthOptions()
            {
                AuthHeaders = new Dictionary<string, string> { {"Complete", "Test"} },
                AuthParams = new Dictionary<string, string> { {"Complete", "Test"} },
                Token = "Complete",
                AuthUrl = "http://www.ably.io",
                Key = "completeKey",
                QueryTime = true,
                AuthCallback = param => null
            };
            otherComplete.Merge(complete);

            Assert.NotEqual(otherComplete.AuthHeaders, complete.AuthHeaders);
            Assert.NotEqual(otherComplete.AuthParams, complete.AuthParams);
            Assert.NotEqual(otherComplete.AuthParams, complete.AuthParams);
            Assert.NotEqual(otherComplete.AuthUrl, complete.AuthUrl);
            Assert.NotEqual(otherComplete.AuthCallback, complete.AuthCallback);
        }

    }
}
