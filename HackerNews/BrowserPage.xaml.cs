using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace HackerNews
{
    public partial class BrowserPage : PhoneApplicationPage
    {
        private NewsItem newsItem;
        private string fullUrl;

        public BrowserPage()
        {
            InitializeComponent();
            NewsItemWebBrowser.IsScriptEnabled = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            newsItem = (NewsItem)DataContext;

            if (newsItem.IsJobAd) // No comments page for a job ad
                ((IApplicationBarIconButton)this.ApplicationBar.Buttons[0]).IsEnabled = false;

            fullUrl = newsItem.IsLocal ? MainPage.BaseUrls[0] + "/" + newsItem.Url : newsItem.Url;

            NewsItemWebBrowser.Navigate(new Uri(fullUrl, UriKind.Absolute));
        }

        private void OpenInIEAppBarMenuItem_Click(object sender, EventArgs e)
        {
            WebBrowserTask wbt = new WebBrowserTask();
            wbt.Uri = new Uri(fullUrl, UriKind.Absolute);
            wbt.Show();
        }

        private void RefreshAppBarMenuItem_Click(object sender, EventArgs e)
        {
            NewsItemWebBrowser.Navigate(new Uri(fullUrl, UriKind.Absolute));
        }

        private void CommentsAppBarIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/CommentsPage.xaml", UriKind.Relative));
            ((FrameworkElement)Application.Current.RootVisual).DataContext = newsItem;
        }

        private void ShareAppBarIconButton_Click(object sender, EventArgs e)
        {
            ShareLinkTask slt = new ShareLinkTask();
            slt.LinkUri = new Uri(fullUrl, UriKind.Absolute);
            slt.Title = newsItem.Title;
            slt.Show();
        }

        private void NewsItemWebBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            LoadingProgressBar.IsIndeterminate = false;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }
    }
}