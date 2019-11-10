using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZoohackathonBackend.Models
{
    public class SearchResult
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("textContext")]
        public string TextContent { get; set; }
        [JsonProperty("prize")]
        public double? Prize { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
