using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        public List<string> currency = new List<string> { "euro", "eur ", "£", "€" };

        // GET: api/Search
        [HttpGet]
        public async Task<IActionResult> GetAsync(string sourceUrl, string searchWord)
        {
            //sourceUrl = "https://www.terraristik.com/tb/list_classifieds.php";
            //searchWord = "snake";

            if (string.IsNullOrEmpty(searchWord))
            {
                return new BadRequestObjectResult("The search word is missing from the query.");
            }

            if (string.IsNullOrEmpty(sourceUrl))
            {
                return new BadRequestObjectResult("The url is missing from the query.");
            }

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

                foreach (var result in searchWordContext)
                {
                    if (!searchContentList.Any(item => item.TextContent == result.TextContent))
                    {
                        searchContentList.Add(result);
                    }

                }
            }

            if (!searchContentList.Any())
            {
                return new NotFoundObjectResult($"No content was found from {sourceUrl} while searching for {searchWord}.");
            }


            return new OkObjectResult(JsonConvert.SerializeObject(searchContentList));
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
                    var sb = new StringBuilder();
                    sb.Append(node.InnerHtml).Replace("<br>", " ").Replace("\n", " ").Replace("\r", " ").Replace("  ", "");

                    SearchResult searchResult = new SearchResult
                    {
                        Url = url,
                        TextContent = sb.ToString().Trim()
                    };
                    if (!resultsList.Any(a => string.Equals(a.TextContent, searchResult.TextContent, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        searchResult = ParsePriceAndCurrency(searchResult);
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

        private SearchResult ParsePriceAndCurrency(SearchResult searchResult)
        {
            foreach (var curren in currency)
            {
                var textToParse = searchResult.TextContent.ToLower();
                var count = Regex.Matches(textToParse, curren).Count;


                if (count == 1)
                {
                    searchResult.Currency = curren;
                    searchResult.Prize = FindPrize(textToParse, curren);


                    break;
                }
            }
            return searchResult;
        }

        private double? FindPrize(string textToParse, string currency)
        {
            var currencyIndex = textToParse.IndexOf(currency);
            var startingIndex = 8;

            if (currencyIndex < startingIndex)
            {
                startingIndex = currencyIndex;
            }

            var textContainingNumber = textToParse.Substring(currencyIndex - startingIndex, startingIndex);

            var numberString = Regex.Split(textContainingNumber, @"[^0-9\.]+").Where(c => c != "." && c.Trim() != "").FirstOrDefault();

            var isDouble = double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out double result);

            if (isDouble)
            {
                return result;
            }

            return null;
        }
    }
}
