using Roadie.Library.Utility;
using Xunit;

namespace Roadie.Library.Tests
{
    public class HashHelperTests
    {
        [Fact]
        public void MD5HandleNullString()
        {
            string s = null;
            Assert.Null(HashHelper.CreateMD5(s));
        }

        [Fact]
        public void MD5HandleNullArray()
        {
            byte[] b = null;
            Assert.Null(HashHelper.CreateMD5(b));
        }

        [Fact]
        public void CreateAndCompareMd5BlankString()
        {
            var s = "";
            var md51 = HashHelper.CreateMD5(s);
            var md52 = HashHelper.CreateMD5(s);
            Assert.Equal(md51, md52);
        }

        [Fact]
        public void CreateAndCompareMd5String()
        {
            var s = "This is a test";
            var md51 = HashHelper.CreateMD5(s);
            var md52 = HashHelper.CreateMD5(s);
            Assert.Equal(md51, md52);
        }

        [Fact]
        public void CreateAndCompareMd5Bytes()
        {
            var sb = System.Text.Encoding.UTF8.GetBytes("This is a test");
            var md51 = HashHelper.CreateMD5(sb);
            var md52 = HashHelper.CreateMD5(sb);
            Assert.Equal(md51, md52);
        }

        [Fact]
        public void CreateAndCompareMd5BytesToString()
        {
            var s = "This is a test";
            var sb = System.Text.Encoding.UTF8.GetBytes("This is a test");
            var md51 = HashHelper.CreateMD5(sb);
            var md52 = HashHelper.CreateMD5(s);
            Assert.Equal(md51, md52);
        }

        [Fact]
        public void CreateAndCompareMd5LongString()
        {
            var s = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";
            var md51 = HashHelper.CreateMD5(s);
            var md52 = HashHelper.CreateMD5(s);
            Assert.Equal(md51, md52);
            Assert.Equal("01aad0e51fcd5582b307613842e4ffe5", md51.ToLower());
        }

        [Fact]
        public void CreateAndEnsureStandardMd5()
        {
            var s = "This is a test";
            var md5 = HashHelper.CreateMD5(s);
            // From https://www.md5hashgenerator.com/
            // From http://onlinemd5.com/
            // From https://md5.online/
            Assert.Equal("ce114e4501d2f4e2dcea3e17b546f339", md5.ToLower());
        }
    }
}