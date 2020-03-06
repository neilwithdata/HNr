using System;
using System.Net;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace HackerNews
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Stack<string>[] nextPrevUrlStacks;
        private DateTime[] lastRefreshed;

        public static readonly string[] BaseUrls = {
            "http://news.ycombinator.com",
            "http://news.ycombinator.com/newest",
            "http://news.ycombinator.com/ask"
        };

        private IApplicationBarIconButton previousAppBarButton;
        private IApplicationBarIconButton refreshAppBarButton;
        private IApplicationBarIconButton nextAppBarButton;

        private WebClient client;

        public MainPage()
        {
            InitializeComponent();

            previousAppBarButton    = (IApplicationBarIconButton)ApplicationBar.Buttons[0];
            refreshAppBarButton     = (IApplicationBarIconButton)ApplicationBar.Buttons[1];
            nextAppBarButton        = (IApplicationBarIconButton)ApplicationBar.Buttons[2];

            // URL stacks start out empty since we're on the 1st page
            nextPrevUrlStacks = new Stack<string>[3];
            for (int i = 0; i < 3; i++)
            {
                nextPrevUrlStacks[i] = new Stack<string>();
                nextPrevUrlStacks[i].Push(string.Empty);
            }

            lastRefreshed = new DateTime[3] {
                DateTime.MinValue,
                DateTime.MinValue,
                DateTime.MinValue
            };

            client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
        }

        private void PerformActionWithDelay(Action myMethod, int delayInMilliseconds)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) => Thread.Sleep(delayInMilliseconds);
            worker.RunWorkerCompleted += (s, e) => myMethod.Invoke();
            worker.RunWorkerAsync();
        }

        private void ShowConnectionError(bool show)
        {
            if (show)
            {
                LoadingProgressBar.Text = "Connection error. Could not fetch news";
                LoadingProgressBar.IsIndeterminate = false;
                LoadingProgressBar.Value = 0;

                SystemTray.ForegroundColor = Colors.Red;
                LoadingProgressBar.IsVisible = true;
            }
            else
            {
                LoadingProgressBar.IsVisible = false;
                LoadingProgressBar.Text = "Fetching news";
                LoadingProgressBar.IsIndeterminate = true;

                SystemTray.ForegroundColor = ((SolidColorBrush)Resources["PhoneForegroundBrush"]).Color;
            }
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            LoadingProgressBar.IsVisible = false;

            if (e.Error != null && !e.Cancelled)
            {
                FlurryWP7SDK.Api.LogError("Error in client_DownloadStringCompleted when fetching news on Main Page", e.Error);

                // Display the connection error text in the SystemTray for 2s
                ShowConnectionError(true);
                PerformActionWithDelay(() => ShowConnectionError(false), 2000);
            }
            else if (!e.Cancelled)
            {
                // Parse the news results on a background thread, and then update the UI
                BackgroundWorker updateNewsWorker = new BackgroundWorker();
                updateNewsWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    args.Result = HackerNewsParser.ParseNewsPage(e.Result);
                };

                updateNewsWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    NewsPage newsPage = (NewsPage) args.Result;

                    int pivotIndx = (int)e.UserState;
                    PivotItem currentPivotItem = (PivotItem)MainPivot.Items[pivotIndx];
                    ListBox currentListBox = (ListBox)currentPivotItem.Content;

                    lastRefreshed[pivotIndx] = DateTime.Now;
                    currentPivotItem.DataContext = newsPage;

                    currentListBox.ItemsSource = null;
                    currentListBox.ItemsSource = newsPage.Items;

                    currentListBox.UpdateLayout();
                    currentListBox.ScrollIntoView(currentListBox.Items[0]);

                    UpdateAppBarLabels();

                };

                updateNewsWorker.RunWorkerAsync();
            }
        }

        private void FetchNews(int currentPivot)
        {
            // cancel any outstanding web requests (WebClient can only handle one connection)
            client.CancelAsync();
            LoadingProgressBar.IsVisible = true;

            try
            {
                Uri newsUri;
                if (nextPrevUrlStacks[currentPivot].Peek() == string.Empty)
                    newsUri = new Uri(BaseUrls[currentPivot]);
                else
                    newsUri = new Uri(BaseUrls[0] + nextPrevUrlStacks[currentPivot].Peek());

                client.DownloadStringAsync(newsUri, currentPivot);
            }
            catch (Exception e)
            {
                FlurryWP7SDK.Api.LogError("Exception calling DownloadStringAsync in FetchNews", e);
            }
        }

        private void Pivot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int currentPivot = MainPivot.SelectedIndex;
            if (DateTime.Equals(lastRefreshed[currentPivot], DateTime.MinValue))
                FetchNews(currentPivot); // refresh if news for this pivot has never been refreshed

            UpdateAppBarLabels();
        }

        private void BackBarIconButton_Click(object sender, EventArgs e)
        {
            nextPrevUrlStacks[MainPivot.SelectedIndex].Pop();
            FetchNews(MainPivot.SelectedIndex);

            UpdateAppBarLabels();
        }

        private void NextBarIconButton_Click(object sender, EventArgs e)
        {
            NewsPage currentContext = (NewsPage) ((PivotItem) MainPivot.SelectedItem).DataContext;
            nextPrevUrlStacks[MainPivot.SelectedIndex].Push(currentContext.NextURLPostfix);
            FetchNews(MainPivot.SelectedIndex);

            UpdateAppBarLabels();
        }

        private void UpdateAppBarLabels()
        {
            int currentPage = nextPrevUrlStacks[MainPivot.SelectedIndex].Count;

            previousAppBarButton.Text = "page " + (currentPage - 1);
            nextAppBarButton.Text = "page " + (currentPage + 1);

            previousAppBarButton.IsEnabled = (currentPage > 1);
            nextAppBarButton.IsEnabled = ((PivotItem)MainPivot.Items[MainPivot.SelectedIndex]).DataContext != null;
        }

        private void RefreshBarIconButton_Click(object sender, EventArgs e)
        {
            FetchNews(MainPivot.SelectedIndex);
            UpdateAppBarLabels();
        }

        private void NewsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ListBox newsListBox = (ListBox)sender;

            if (newsListBox.SelectedIndex == -1) return; // the selection was cleared so do nothing
            else
            {
                // A news item was selected - launch the browser page to display the story
                NewsItem selectedNewsItem = (NewsItem)newsListBox.SelectedItem;
                NavigationService.Navigate(new Uri("/BrowserPage.xaml", UriKind.Relative));
                ((FrameworkElement)Application.Current.RootVisual).DataContext = selectedNewsItem;

                // clear the selection
                newsListBox.SelectedIndex = -1;
            }
        }

        private void CommentIcon_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NewsItem selectedNewsItem = (NewsItem)((Grid)sender).DataContext;

            NavigationService.Navigate(new Uri("/CommentsPage.xaml", UriKind.Relative));
            ((FrameworkElement)Application.Current.RootVisual).DataContext = selectedNewsItem;
            
            e.Handled = true;
        }

        private void AboutAppBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }
    }
}