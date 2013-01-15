using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ably.Tests
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
                AuthHeaders = new List<string> { "Test" },
                AuthParams = new List<string> { "Test" },
                AuthToken = "Token",
                AuthUrl = "http://www.google.com",
                Key = "key",
                KeyId = "keyId",
                KeyValue = "keyValue",
                QueryTime = true,
                AuthCallback = param => string.Empty
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
            
            Assert.Equal(blankOptions.KeyId, complete.KeyId);
            Assert.Equal(blankOptions.KeyValue, complete.KeyValue);
            Assert.Equal(blankOptions.QueryTime, complete.QueryTime);
        }

        [Fact]
        public void Merge_WithOptionsNotSet_DoesNotOverwriteKey()
        {
            AuthOptions complete = GetCompleteOptions();
            var blankOptions = GetBlankOptions();
            blankOptions.Merge(complete);

            Assert.NotEqual(blankOptions.Key, complete.Key);
        }

        [Fact]
        public void Merge_WithCompleteOptions_DoesNotOverwriteAnything()
        {
            AuthOptions complete = GetCompleteOptions();
            var otherComplete = new AuthOptions()
            {
                AuthHeaders = new List<string> { "Complete" },
                AuthParams = new List<string> { "Complete" },
                AuthToken = "Complete",
                AuthUrl = "http://www.ably.io",
                Key = "completeKey",
                KeyId = "CompleteKeyId",
                KeyValue = "CompleteKeyValue",
                QueryTime = true,
                AuthCallback = param => "Complete"
            };
            otherComplete.Merge(complete);

            Assert.NotEqual(otherComplete.AuthHeaders, complete.AuthHeaders);
            Assert.NotEqual(otherComplete.AuthParams, complete.AuthParams);
            Assert.NotEqual(otherComplete.AuthParams, complete.AuthParams);
            Assert.NotEqual(otherComplete.AuthUrl, complete.AuthUrl);
            Assert.NotEqual(otherComplete.AuthCallback, complete.AuthCallback);
            Assert.NotEqual(otherComplete.KeyId, complete.KeyId);
            Assert.NotEqual(otherComplete.KeyValue, complete.KeyValue);
        }

    }
}
