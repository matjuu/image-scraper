using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ImageScraper
{
    public class TarkovScaper
    {
        private ScrapingBrowser _browser;
        private Uri _baseUri;

        public TarkovScaper(string baseUrl)
        {
            _browser = new ScrapingBrowser();
            _baseUri = new Uri(baseUrl);
        }

        public List<string> ScrapeWikiMenuForCategories()
        {
            var page = _browser.NavigateToPage(_baseUri);
            var html = page.Html;

            var categoryUrls = new List<string>();

            var gearCategories = html.CssSelect("div#p-Gear > div.body > ul > li > a");
            var itemCategories = html.CssSelect("div#p-Items > div.body > ul > li > a");

            categoryUrls.AddRange(gearCategories.Select(x => x.Attributes["href"].Value));
            categoryUrls.AddRange(itemCategories.Select(x => x.Attributes["href"].Value));

            // Removing non-tradeable items
            categoryUrls.Remove("/wiki/Armbands");
            categoryUrls.Remove("/wiki/Secure_containers");
            categoryUrls.Remove("/wiki/Tactical_clothing");
            categoryUrls.Remove("/wiki/Currency");

            //Ammunition has it's own method
            categoryUrls.Remove("/wiki/Ammunition");

            return categoryUrls;
        }

        public List<ScrapedItem> ScrapeAmmunitionItems()
        {
            var html = GetPageHtml("/wiki/Ammunition");
            var ammoRows = html.CssSelect("table.wikitable > tbody > tr");

            var ammoLinks = new List<string>();

            foreach (var ammoRow in ammoRows)
            {
                var links = ammoRow.CssSelect("td > a");
                if (links.Count() > 0) ammoLinks.Add(links.First().Attributes["href"].Value);
            }

            //Not sold in shop
            ammoLinks.Remove("/wiki/30x29mm");

            var images = new List<ScrapedItem>();

            foreach (var ammoLink in ammoLinks)
            {
                var itemHtml = GetPageHtml(ammoLink);
                var tableRows = itemHtml.CssSelect("table.wikitable > tbody > tr");
                foreach (var tableRow in tableRows)
                {
                    var cells = tableRow.CssSelect("th > a").ToList();

                    if (cells.Count > 1) 
                    {
                        var itemName = cells[1].InnerHtml;
                        var imageUrl = cells.CssSelect("img").First().Attributes["src"].Value;

                        images.Add(new ScrapedItem { Name = itemName, ImageUrl = imageUrl });
                    }
                }
            }

            return images;
        }


        public List<ScrapedItem> ScrapeItems(string path)
        {
            var images = new List<ScrapedItem>();

            var html = GetPageHtml(path);

            var tableRows = html.CssSelect("table.wikitable > tbody > tr");
            foreach (var tableRow in tableRows)
            {
                if (tableRow.LastChild.Name == "th") continue; //Skips header rows

                var cells = tableRow.ChildNodes.Where(node => node.Name != "#text").ToList();
                //.CssSelect("a").ToList();


                //Weapons have title/image swapped weirdly
                if (path == "/wiki/Weapons")
                {
                    ;
                    var itemName = WebUtility.HtmlDecode(cells[0].InnerText.Replace("\n", null));
                    var imageUrl = cells[1].CssSelect("img").First().Attributes["src"].Value;
                    images.Add(new ScrapedItem { Name = itemName, ImageUrl = imageUrl });
                }
                else
                {
                    cells = cells.CssSelect("a").ToList();

                    if (cells.Count > 1)
                    {
                        var itemName = cells[1].InnerText;
                        var imageUrl = cells[0].CssSelect("img").First().Attributes["src"].Value;
                        images.Add(new ScrapedItem { Name = itemName, ImageUrl = imageUrl });
                    }
                }
            }

            return images;
        }

        public (MemoryStream, string) DownloadImage(string imageUrl)
        {
            var resource = _browser.DownloadWebResource(new Uri(imageUrl));
            return (resource.Content, resource.ContentType);
        }

        private HtmlNode GetPageHtml(string path)
        {
            WebPage webPage = _browser.NavigateToPage(_baseUri.Combine(path));
            return webPage.Html;
        }
    }

    public class ScrapedItem
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string ImageType { get; set; }
    }
}
