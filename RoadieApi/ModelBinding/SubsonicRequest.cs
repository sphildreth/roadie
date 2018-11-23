using Microsoft.AspNetCore.Mvc;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.ModelBinding
{
    [ModelBinder(BinderType = typeof(SubsonicRequestBinder))]
    public class SubsonicRequest : Request
    {
    }
}
