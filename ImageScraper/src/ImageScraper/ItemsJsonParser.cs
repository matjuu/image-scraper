using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageScraper
{
    public static class ItemsJsonParser
    {
        public static Lookup<string, string> GetItemNameToIdLookup()
        {
            var json = JObject.Parse(File.ReadAllText("./Items/items.json"));
            var items = json.ToObject<Items>();
            return (Lookup<string, string>)items.Item.ToLookup(x => x.Name, x => x.Id);
        }

        private class Items
        {
            public IEnumerable<Item> Item { get; set; }
        }

        private class Item
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
