using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageScraper
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Scraping for items");
            var scraper = new TarkovScaper("https://escapefromtarkov.fandom.com");
            var items = new List<ScrapedItem>();

            var categories = scraper.ScrapeWikiMenuForCategories();

            foreach (var category in categories)
            {
                var categoryItems = scraper.ScrapeItems(category);
                items.AddRange(categoryItems);
            }

            items.AddRange(scraper.ScrapeAmmunitionItems());

            Console.WriteLine("Downloading items");
            var outputPath = $"images/{DateTime.UtcNow.Ticks}";
            var itemNameToId = ItemsJsonParser.GetItemNameToIdLookup();
            Directory.CreateDirectory(outputPath);
            var progressCounter = 0;
            var itemCount = items.Count;
            var unlabeledImages = 0;
            foreach (var item in items)
            {
                var lookup = itemNameToId[item.Name];
                string fileName;

                if(lookup.Count() > 0)
                {
                    fileName = lookup.First();
                }
                else
                {
                    fileName = item.Name;
                    unlabeledImages++;
                }
                fileName = fileName.Replace("\\", " ").Replace("/", " ").Replace("\"", null);

                var (stream, fileType) = scraper.DownloadImage(item.ImageUrl);

                string fileExtension = string.Empty;
                switch (fileType)
                {
                    case "image/png":
                        fileExtension = "png";
                        break;
                    case "image/gif":
                        fileExtension = "gif";
                        break;
                    case "image/jpeg":
                        fileExtension = "jpeg";
                        break;
                    default:
                        throw new Exception($"{fileType} is an unhandled filetype. Cannot save this file.");
                }

                var fileStream = File.OpenWrite($"{outputPath}/{fileName}.{fileExtension}");
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
                finally
                {
                    fileStream.Close();
                }

                progressCounter++;
                if (progressCounter % 100 == 0)
                {
                    Console.WriteLine($"Progress: {progressCounter * 100.0 / itemCount}.");
                }
            }
            Console.WriteLine($"Unlabeled images: {unlabeledImages}.");
        }

    }
}
