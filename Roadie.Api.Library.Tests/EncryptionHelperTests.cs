using Roadie.Library.Utility;
using System;
using Xunit;

namespace Roadie.Library.Tests
{
    public class EncryptionHelperTests
    {
        [Fact]
        public void EncryptAndDecrypt()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            var encrypted = EncryptionHelper.Encrypt(value, key);
            Assert.NotNull(encrypted);
            Assert.NotEqual(encrypted, value);

            var decrypted = EncryptionHelper.Decrypt(encrypted, key);
            Assert.NotNull(decrypted);
            Assert.Equal(decrypted, value);
        }
    }
}