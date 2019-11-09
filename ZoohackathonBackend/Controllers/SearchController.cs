using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ZoohackathonBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        // GET: api/Search
        [HttpGet]
        public async Task<IEnumerable<string>> GetAsync(string sourceUrl, string searchWord)
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

            var searchContentList = new List<string>();

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
                searchContentList.AddRange(searchWordContext);
            }

            return searchContentList;
        }

        private async Task<List<string>> GetContentAsync(HttpClient client, string sourceUrl, string queryParam, string searchWord)
        {

            var response = await client.GetAsync(sourceUrl + "?" + queryParam);
            var pageContents = await response.Content.ReadAsStringAsync();

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContents);
            var contentArray = new List<string>();
            var anchorNodes = htmlDoc.DocumentNode.SelectNodes("//p");


            var count = Regex.Matches(pageContents, searchWord).Count;

            List<string> resultsList = new List<string>();
            foreach (var node in anchorNodes)
            {
                if (node.InnerHtml.ToLower().Contains(searchWord.ToLower()))
                {
                    resultsList.Add(node.InnerHtml.Trim().Replace("  ", ""));
                }
            }

            if(!resultsList.Any())
            {
                return null;
            }

            return resultsList;
        }
    }
}
