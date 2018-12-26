using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.Imaging.BingModels
{
    [Serializable]
    public class BingImageResult
    {
        public string _type { get; set; }
        public bool? displayRecipeSourcesBadges { get; set; }
        public bool? displayShoppingSourcesBadges { get; set; }
        public Instrumentation instrumentation { get; set; }
        public int nextOffsetAddCount { get; set; }
        public List<Pivotsuggestion> pivotSuggestions { get; set; }
        public List<Queryexpansion> queryExpansions { get; set; }
        public int? totalEstimatedMatches { get; set; }
        public List<Value> value { get; set; }
        public string webSearchUrl { get; set; }
    }

    public class Insightssourcessummary
    {
        public int? recipeSourcesCount { get; set; }
        public int? shoppingSourcesCount { get; set; }
    }

    [Serializable]
    public class Instrumentation
    {
        public string pageLoadPingUrl { get; set; }
    }

    public class Pivotsuggestion
    {
        public string pivot { get; set; }
        public List<Suggestion> suggestions { get; set; }
    }

    public class Queryexpansion
    {
        public string displayText { get; set; }
        public string searchLink { get; set; }
        public string text { get; set; }
        public Thumbnail1 thumbnail { get; set; }
        public string webSearchUrl { get; set; }
    }

    public class Suggestion
    {
        public string displayText { get; set; }
        public string searchLink { get; set; }
        public string text { get; set; }
        public Thumbnail2 thumbnail { get; set; }
        public string webSearchUrl { get; set; }
    }

    public class Thumbnail
    {
        public int? height { get; set; }
        public int? width { get; set; }
    }

    public class Thumbnail1
    {
        public string thumbnailUrl { get; set; }
    }

    public class Thumbnail2
    {
        public string thumbnailUrl { get; set; }
    }

    [Serializable]
    public class Value
    {
        public string accentColor { get; set; }
        public string contentSize { get; set; }
        public string contentUrl { get; set; }
        public DateTime? datePublished { get; set; }
        public string encodingFormat { get; set; }
        public int? height { get; set; }
        public string hostPageDisplayUrl { get; set; }
        public string hostPageUrl { get; set; }
        public string imageId { get; set; }
        public string imageInsightsToken { get; set; }
        public Insightssourcessummary insightsSourcesSummary { get; set; }
        public string name { get; set; }
        public Thumbnail thumbnail { get; set; }
        public string thumbnailUrl { get; set; }
        public string webSearchUrl { get; set; }
        public int? width { get; set; }
    }
}