using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ZoohackathonBackend.Models;

namespace ZoohackathonBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        // GET: api/Search
        [HttpGet]
        public async Task<string> GetAsync(string sourceUrl, string searchWord)
        {
            //sourceUrl = "https://www.terraristik.com/tb/list_classifieds.php";
            //searchWord = "snake";

            List<string> links = new List<string>();

            HttpClient client = new HttpClient();
            var response = await client.GetAsync(sourceUrl);
            var pageContents = await response.Content.ReadAsStringAsync();

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContents);

            var contentArray = new List<string>();
            var anchorNodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (anchorNodes != null)
            {
                foreach (var anchorNode in anchorNodes)
                {
                    string blaa = anchorNode.GetAttributeValue("href", "");
                    if (!contentArray.Contains(blaa))
                    {
                        contentArray.Add(blaa);
                    }
                }
            }

            var searchContentList = new List<SearchResult>();

            foreach (var link in contentArray.Where(a => a.Contains("?")))
            {
                var splitted = link.Split('?');
                var urlmelkein = splitted[1].Split('>');
                var queryParam = urlmelkein[0];
                if (queryParam.Contains(" "))
                {
                    continue;
                }
                var searchWordContext = await GetContentAsync(client, sourceUrl, queryParam, searchWord);
                if (searchWordContext == null) continue;

                foreach(var result in searchWordContext)
                {
                    if(!searchContentList.Any(item => item.TextContent == result.TextContent))
                    {
                        searchContentList.Add(result);
                    }

                }
            }

            return JsonConvert.SerializeObject(searchContentList);
        }

        private async Task<List<SearchResult>> GetContentAsync(HttpClient client, string sourceUrl, string queryParam, string searchWord)
        {
            var url = sourceUrl + "?" + queryParam;
            var response = await client.GetAsync(url);
            var pageContents = await response.Content.ReadAsStringAsync();

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContents);
            var contentArray = new List<string>();
            var anchorNodes = htmlDoc.DocumentNode.SelectNodes("//p");


            var count = Regex.Matches(pageContents, searchWord).Count;

            List<SearchResult> resultsList = new List<SearchResult>();
            foreach (var node in anchorNodes)
            {
                if (node.InnerHtml.ToLower().Contains(searchWord.ToLower()))
                {
                    SearchResult searchResult = new SearchResult
                    {
                        Url = url,
                        TextContent = node.InnerHtml.Trim().Replace("<br>", "").Replace("\n", "").Replace("\r", "").Replace("  ", "")
                    };
                    if (!resultsList.Any(a => string.Equals(a.TextContent, searchResult.TextContent, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        resultsList.Add(searchResult);
                    }
                }
            }

            if (!resultsList.Any())
            {
                return null;
            }

            return resultsList;
        }
    }
}
