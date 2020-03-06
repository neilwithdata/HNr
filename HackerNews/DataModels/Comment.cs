using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HackerNews
{
    public class Comment
    {
        public string PostedBy { get; set; }
        public string PostedAgo { get; set; }
        public string CommentText { get; set; }
        public int Id { get; set; }
        public int Points { get; set; }
        public int ParentId { get; set; }
        public int PostId { get; set; }
        public List<Comment> Children { get; set; }
        public int IndentationLevel { get; set; }
    }
}
