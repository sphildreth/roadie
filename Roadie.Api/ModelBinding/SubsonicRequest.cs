using Microsoft.AspNetCore.Mvc;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;

namespace Roadie.Api.ModelBinding
{
    [ModelBinder(BinderType = typeof(SubsonicRequestBinder))]
    public class SubsonicRequest : Request
    {
    }
}
