namespace Roadie.Library.Encoding
{
    public interface IHttpEncoder
    {
        string HtmlEncode(string s);

        string UrlDecode(string s);

        string UrlEncode(string s);
    }
}