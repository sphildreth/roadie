using System;
using System.Collections.Generic;

namespace Roadie.Library.SearchEngines.Imaging
{
    public class ITunesSearchResult
    {
        public int resultCount { get; set; }
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public string wrapperType { get; set; }
        public string collectionType { get; set; }
        public int artistId { get; set; }
        public int collectionId { get; set; }
        public int amgArtistId { get; set; }
        public string artistName { get; set; }
        public string artistType { get; set; }
        public string collectionName { get; set; }
        public string collectionCensoredName { get; set; }
        public string artistViewUrl { get; set; }
        public string artistLinkUrl { get; set; }
        public string collectionViewUrl { get; set; }
        public string artworkUrl60 { get; set; }
        public string artworkUrl100 { get; set; }
        public float collectionPrice { get; set; }
        public string collectionExplicitness { get; set; }
        public int trackCount { get; set; }
        public string copyright { get; set; }
        public string country { get; set; }
        public string currency { get; set; }
        public DateTime releaseDate { get; set; }
        public string primaryGenreName { get; set; }
    }
}