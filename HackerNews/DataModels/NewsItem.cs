using System.Runtime.Serialization;

namespace HackerNews
{
    public class NewsItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public int Id { get; set; }
        public int CommentCount { get; set; }
        public int Points { get; set; }
        public string PostedAgo { get; set; }
        public string PostedBy { get; set; }
        public bool IsJobAd { get; set; }
        public bool IsLocal { get; set; }
    }
}