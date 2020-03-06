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

namespace HackerNews
{
    public abstract class DataTemplateSelector : ContentControl
    {
        public virtual DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return null;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            ContentTemplate = SelectTemplate(newContent, this);
        }
    }


    public class NewsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NewsItemTemplate
        {
            get;
            set;
        }

        public DataTemplate JobAdTemplate
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            NewsItem newsItem = (NewsItem)item;

            if (newsItem != null)
            {
                if (newsItem.IsJobAd)
                    return JobAdTemplate;
                else
                    return NewsItemTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
