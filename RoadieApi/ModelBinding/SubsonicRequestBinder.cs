using Microsoft.AspNetCore.Mvc.ModelBinding;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.ModelBinding
{
    /// <summary>
    /// This is needed as some clienst post some get, some query string some body post.
    /// </summary>
    public class SubsonicRequestBinder : IModelBinder
    {

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }
            var queryDictionary = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(bindingContext.HttpContext.Request.QueryString.ToString());

            // Create a dictionary of all the properties to populate on the result model
            var modelDictionary = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "u", null },
                { "p", null },
                { "s", null },
                { "t", null },
                { "v", null },
                { "c", null },
                { "id", null },
                { "f", null },
                { "callback", null },
                { "musicFolderId", null },
                { "albumCount", null },
                { "albumOffset", null },
                { "artistCount", null },
                { "artistOffset", null },
                { "fromYear", null },
                { "genre", null },
                { "offset", null },
                { "query", null },
                { "size", null },
                { "songCount", null },
                { "songOffset", null },
                { "toYear", null },
                { "type", null }
            };

            // Setup model dictionary from Query Parameters
            modelDictionary["albumCount"] = queryDictionary.ContainsKey("albumCount") ? SafeParser.ToNumber<int?>(queryDictionary["albumCount"].First()) : null;
            modelDictionary["albumOffset"] = queryDictionary.ContainsKey("albumOffset") ? SafeParser.ToNumber<int?>(queryDictionary["albumOffset"].First()) : null;
            modelDictionary["artistCount"] = queryDictionary.ContainsKey("artistCount") ? SafeParser.ToNumber<int?>(queryDictionary["artistCount"].First()) : null;
            modelDictionary["artistOffset"] = queryDictionary.ContainsKey("artistOffset") ? SafeParser.ToNumber<int?>(queryDictionary["artistOffset"].First()) : null;
            modelDictionary["c"] = queryDictionary.ContainsKey("c") ? queryDictionary["c"].First() : null;
            modelDictionary["callback"] = queryDictionary.ContainsKey("callback") ? queryDictionary["callback"].First() : null;
            modelDictionary["f"] = queryDictionary.ContainsKey("f") ? queryDictionary["f"].First() : null;
            modelDictionary["fromYear"] = queryDictionary.ContainsKey("fromYear") ? SafeParser.ToNumber<int?>(queryDictionary["fromYear"].First()) : null;
            modelDictionary["genre"] = queryDictionary.ContainsKey("genre") ? queryDictionary["genre"].First() : null;
            modelDictionary["id"] = queryDictionary.ContainsKey("id") ? queryDictionary["id"].First() : null;
            modelDictionary["musicFolderId"] = queryDictionary.ContainsKey("musicFolderId") ? SafeParser.ToNumber<int?>(queryDictionary["musicFolderId"].First()) : null;
            modelDictionary["offset"] = queryDictionary.ContainsKey("offset") ? SafeParser.ToNumber<int?>(queryDictionary["offset"].First()) : null;
            modelDictionary["p"] = queryDictionary.ContainsKey("p") ? queryDictionary["p"].First() : null;
            modelDictionary["query"] = queryDictionary.ContainsKey("query") ? queryDictionary["query"].First() : null;
            modelDictionary["s"] = queryDictionary.ContainsKey("s") ? queryDictionary["s"].First() : null;
            var size = queryDictionary.ContainsKey("size") ? SafeParser.ToNumber<int?>(queryDictionary["size"].First()) : null;
            var count = queryDictionary.ContainsKey("count") ? SafeParser.ToNumber<int?>(queryDictionary["count"].First()) : null;
            modelDictionary["size"] = size ?? count ?? 20;
            modelDictionary["songCount"] = queryDictionary.ContainsKey("songCount") ? SafeParser.ToNumber<int?>(queryDictionary["songCount"].First()) : null;
            modelDictionary["songOffset"] = queryDictionary.ContainsKey("songOffset") ? SafeParser.ToNumber<int?>(queryDictionary["songOffset"].First()) : null;
            modelDictionary["t"] = queryDictionary.ContainsKey("t") ? queryDictionary["t"].First() : null;
            modelDictionary["toYear"] = queryDictionary.ContainsKey("toYear") ? SafeParser.ToNumber<int?>(queryDictionary["toYear"].First()) : null;
            modelDictionary["type"] = queryDictionary.ContainsKey("type") ? SafeParser.ToEnum<ListType>(queryDictionary["type"].First()) : ListType.AlphabeticalByName;
            modelDictionary["u"] = queryDictionary.ContainsKey("u") ? queryDictionary["u"].First() : null;
            modelDictionary["v"] = queryDictionary.ContainsKey("v") ? queryDictionary["v"].First() : null;

            // Setup model dictionary from Posted Body values
            if (!bindingContext.HttpContext.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(bindingContext.HttpContext.Request.ContentType))
            {
                var formCollection = bindingContext.HttpContext.Request.Form;
                if (formCollection != null && formCollection.Any())
                {
                    foreach (var form in formCollection)
                    {
                        if (modelDictionary.ContainsKey(form.Key))
                        {
                            modelDictionary[form.Key] = form.Value.FirstOrDefault();
                        }
                    }
                }
            }

            var model = new SubsonicRequest
            {
                AlbumCount = SafeParser.ToNumber<int?>(modelDictionary["albumCount"]) ?? 20,
                AlbumOffset = SafeParser.ToNumber<int?>(modelDictionary["albumOffset"]),
                ArtistCount = SafeParser.ToNumber<int?>(modelDictionary["artistCount"]) ?? 20,
                ArtistOffset = SafeParser.ToNumber<int?>(modelDictionary["artistOffset"]),
                c = SafeParser.ToString(modelDictionary["c"]),
                callback = SafeParser.ToString(modelDictionary["callback"]),
                f = SafeParser.ToString(modelDictionary["f"]),
                FromYear = SafeParser.ToNumber<int?>(modelDictionary["fromYear"]),
                Genre = SafeParser.ToString(modelDictionary["genre"]),
                id = SafeParser.ToString(modelDictionary["id"]),
                MusicFolderId = SafeParser.ToNumber<int?>(modelDictionary["musicFolderId"]),
                Offset = SafeParser.ToNumber<int?>(modelDictionary["offset"]),
                p = SafeParser.ToString(modelDictionary["p"]),
                Query = SafeParser.ToString(modelDictionary["query"]),
                s = SafeParser.ToString(modelDictionary["s"]),
                Size = SafeParser.ToNumber<int?>(modelDictionary["size"]),
                SongCount = SafeParser.ToNumber<int?>(modelDictionary["songCount"]) ?? 20,
                SongOffset = SafeParser.ToNumber<int?>(modelDictionary["songOffset"]),
                t = SafeParser.ToString(modelDictionary["t"]),
                ToYear = SafeParser.ToNumber<int?>(modelDictionary["toYear"]),
                Type = SafeParser.ToEnum<ListType>(modelDictionary["type"]),
                u = SafeParser.ToString(modelDictionary["u"]),
                v = SafeParser.ToString(modelDictionary["v"])
            };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
