using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace HackerNews
{
    public class CommentUtils
    {
        public const float IndentationDarkenFactor = 0.15F;

        public static Color getRGBDarkness(int indentationLevel, byte r, byte g, byte b)
        {
            // Never want to darken by more than a factor of 1
            float darkenByFactor = Math.Min(IndentationDarkenFactor * indentationLevel, 1.0F);

            byte newR = (byte)(r * (1.0F - darkenByFactor));
            byte newG = (byte)(g * (1.0F - darkenByFactor));
            byte newB = (byte)(b * (1.0F - darkenByFactor));

            return Color.FromArgb(255, newR, newG, newB);
        }

        /// <summary>
        /// Converts a given string comment in to a displayable read-only RichTextBox
        /// </summary>
        public static UIElement UIElementFromComment(Comment comment)
        {
            StackPanel mainPanel = new StackPanel()
            {
                Margin = new Thickness(0)
            };

            Border border = new Border()
            {
                BorderBrush = new SolidColorBrush(getRGBDarkness(comment.IndentationLevel, 255, 102, 0)),
                BorderThickness = new Thickness(5, 0, 0, 0),
                Margin = new Thickness(comment.IndentationLevel * 25, 5, 0, 5),
            };

            border.Child = mainPanel;

            StackPanel commHeaderPanel = new StackPanel();
            commHeaderPanel.Orientation = Orientation.Horizontal;

            TextBlock postedBy = new TextBlock()
            {
                Margin = new Thickness(12, 0, 3, 0),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 240, 150, 9)),
                FontSize = (double)Application.Current.Resources["PhoneFontSizeSmall"]
            };
            postedBy.Text = comment.PostedBy;

            TextBlock postedAgo = new TextBlock()
            {
                Style = (Style)Application.Current.Resources["PhoneTextSubtleStyle"],
                FontSize = (double)Application.Current.Resources["PhoneFontSizeSmall"]
            };
            postedAgo.Text = comment.PostedAgo;

            commHeaderPanel.Children.Add(postedBy);
            commHeaderPanel.Children.Add(postedAgo);

            mainPanel.Children.Add(commHeaderPanel);

            RichTextBox textBlock = new RichTextBox()
            {
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };

            Paragraph currentParagraph = new Paragraph();
            Span spanInline = null;

            char[] run = new char[comment.CommentText.Length];
            char[] tag = new char[1000];
            int tagIndx = 0;
            int runIndx = 0;
            bool inside = false;
            bool code = false;

            for (int i = 0; i < comment.CommentText.Length; i++)
            {
                char let = comment.CommentText[i];
                if (let == '<')
                {
                    // Write out everything up till now to the current paragraph
                    if (spanInline == null)
                    {
                        Run r = new Run()
                        {
                            Text = HttpUtility.HtmlDecode(new string(run, 0, runIndx))
                        };

                        if (code)
                        {
                            r.FontFamily = new FontFamily("Courier New");
                            code = false;
                        }

                        currentParagraph.Inlines.Add(r);
                    }
                    else
                    {
                        spanInline.Inlines.Add(new Run() { Text = HttpUtility.HtmlDecode(new string(run, 0, runIndx)) });
                        currentParagraph.Inlines.Add(spanInline);
                        spanInline = null;
                    }

                    tagIndx = 0;

                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    string tagStr = new string(tag, 0, tagIndx).Trim().ToLower();

                    switch (tagStr)
                    {
                        case "p":
                            currentParagraph.Inlines.Add(new LineBreak());
                            currentParagraph.Inlines.Add(new LineBreak());
                            break;
                        case "i":
                            spanInline = new Italic();
                            break;
                        case "code":
                            code = true;
                            break;
                        default:
                            break; // unknown tag - do not copy to output
                    }

                    if (tagStr.StartsWith("a href"))
                    {
                        // determine the URL
                        int urlStartIndx = tagStr.IndexOf("a href") + 8;
                        int urlEndIndx = tagStr.IndexOf("\"", urlStartIndx);

                        spanInline = new Hyperlink()
                        {
                            NavigateUri = new Uri(tagStr.Substring(urlStartIndx, urlEndIndx - urlStartIndx)),
                            TargetName = "_blank",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 27, 161, 226))
                        };
                    }

                    runIndx = 0;

                    inside = false;
                    continue;
                }
                if (inside)
                {
                    tag[tagIndx++] = let;
                }
                else
                {
                    run[runIndx++] = let;
                }
            }

            // Write out whatever is left
            currentParagraph.Inlines.Add(new Run() { Text = HttpUtility.HtmlDecode(new string(run, 0, runIndx)) });

            textBlock.Blocks.Add(currentParagraph);

            mainPanel.Children.Add(textBlock);

            return border;
        }
    }
}
