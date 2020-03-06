using System.Net;
using HtmlAgilityPack;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System;

namespace HackerNews
{
    public class HackerNewsParser
    {
        public static NewsPage ParseNewsPage(string htmlString)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString);

            // LINQ Query to obtain the news story title and URL on the front page
            var titlesOnPage = from node in document.DocumentNode.Descendants()
                                where node.Name == "td" &&
                                node.Attributes["class"] != null &&
                                node.Attributes["class"].Value == "title" &&
                                node.FirstChild.Name == "a"
                                select node.FirstChild;

            NewsPage newsPage = new NewsPage();
            List<NewsItem> newsItemList = new List<NewsItem>();

            foreach (HtmlNode n in titlesOnPage)
            {
                NewsItem i = new NewsItem();

                try
                {
                    i.Title = HttpUtility.HtmlDecode(n.InnerText.Trim());
                    i.Url = n.Attributes["href"].Value;
                    i.IsLocal = i.Url.StartsWith("item?");

                    if (i.Title == "More")
                    {
                        newsPage.NextURLPostfix = i.Url;
                        if (!newsPage.NextURLPostfix.StartsWith("/"))
                            newsPage.NextURLPostfix = "/" + newsPage.NextURLPostfix;

                        break;
                    }
                }
                catch (Exception exception)
                {
                    FlurryWP7SDK.Api.LogError("Exception parsing news story title/url. Title = " + i.Title + "; URL = " + i.Url, exception);
                }

                newsItemList.Add(i);
            }

            // LINQ Query to obtain the Id, CommentCount, Points, PostedAgo, and PostedBy for each
            // story on the front page
            var storySubtextsOnPage = from node in document.DocumentNode.Descendants()
                                      where node.Name == "td" &&
                                      node.Attributes["class"] != null &&
                                      node.Attributes["class"].Value == "subtext"
                                      select node;

            int cntr = 0;
            foreach (HtmlNode n in storySubtextsOnPage)
            {
                NewsItem i = newsItemList[cntr];

                try
                {
                    if (n.ChildNodes.Count == 1)
                    {
                        // This is a job ad - all that is displayed is how long ago posted
                        i.Points = 0;
                        i.PostedAgo = n.ChildNodes[0].InnerText;
                        i.PostedBy = string.Empty;
                        i.CommentCount = 0;
                        i.Id = -1;
                        i.IsJobAd = true;
                    }
                    else
                    {
                        string pointsStr = n.ChildNodes[0].InnerText.Trim();
                        i.Points = int.Parse(pointsStr.Split()[0]);

                        i.PostedBy = n.ChildNodes[2].InnerText;

                        i.PostedAgo = n.ChildNodes[3].InnerText.Trim(new char[] { ' ', '|' });

                        string commentsStr = n.ChildNodes[4].InnerText.Trim();
                        if (commentsStr == "discuss") i.CommentCount = 0;
                        else i.CommentCount = int.Parse(commentsStr.Split()[0]);

                        string urlStr = n.ChildNodes[4].Attributes["href"].Value;
                        i.Id = int.Parse(urlStr.Substring(urlStr.IndexOf("=") + 1));
                    }
                }
                catch (Exception exception)
                {
                    FlurryWP7SDK.Api.LogError("Exception parsing news story subtext. Title = " + i.Title + "; URL = " + i.Url, exception);
                }

                cntr++;
            }

            newsPage.Items = newsItemList;

            return newsPage;
        }

        public static List<Comment> ParseCommentsPage(string htmlString)
        {
            List<Comment> comments = new List<Comment>();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString);

            var tdBodyNodes = from node in document.DocumentNode.Descendants()
                               where node.Name == "td" &&
                               node.Attributes["class"] != null &&
                               node.Attributes["class"].Value == "default"
                               select node;

            foreach (HtmlNode n in tdBodyNodes)
            {
                try
                {
                    Comment c = new Comment();

                    // Parse out the comment indentation level
                    HtmlNode spacerNode = n.PreviousSibling.PreviousSibling.FirstChild;
                    c.IndentationLevel = int.Parse(spacerNode.Attributes["width"].Value) / 40;

                    // Parse out the comment text
                    var commentSpans = from node in n.Descendants()
                                       where node.Name == "span" &&
                                       node.Attributes["class"] != null &&
                                       node.Attributes["class"].Value == "comment"
                                       select node;

                    StringBuilder commentStrBuilder = new StringBuilder();
                    foreach (HtmlNode commentSpan in commentSpans)
                    {
                        commentStrBuilder.Append(commentSpan.FirstChild.InnerHtml);
                        commentStrBuilder.Append("<p>");
                    }

                    commentStrBuilder.Remove(commentStrBuilder.Length - 3, 3);

                    c.CommentText = commentStrBuilder.ToString();

                    // Comments that have been deleted by mods are flagged as [dead]
                    if (c.CommentText == "[dead]")
                        continue;

                    // Parse out PostedBy
                    HtmlNode hrefHeaderNode = n.FirstChild.FirstChild.FirstChild;
                    c.PostedBy = hrefHeaderNode.InnerText;

                    // Parse out PostedAgo
                    c.PostedAgo = hrefHeaderNode.NextSibling.InnerText.Trim(new char[] { ' ', '|' });

                    comments.Add(c);
                }
                catch (Exception exception)
                {
                    FlurryWP7SDK.Api.LogError("Caught exception parsing comment in story", exception);
                }
            }

            return comments;
        }
    }
}
