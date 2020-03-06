using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HackerNews
{
    public class NewsPage
    {
        public string NextURLPostfix { get; set; }
        public List<NewsItem> Items { get; set; }
    }
}