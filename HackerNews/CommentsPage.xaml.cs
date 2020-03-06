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
using System.Diagnostics;
using System.ComponentModel;

namespace HackerNews
{
    public partial class CommentsPage : PhoneApplicationPage
    {
        private WebClient client;
        private LoadingProgressControl loadingDialog;

        public CommentsPage()
        {
            InitializeComponent();

            client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null && !e.Cancelled)
            {
                FlurryWP7SDK.Api.LogError("Error in client_DownloadStringCompleted when fetching comments for story", e.Error);
            }
            else if (!e.Cancelled)
            {
                // Parse the comments on a background thread, and then update the UI
                BackgroundWorker updateCommentsWorker = new BackgroundWorker();
                updateCommentsWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    args.Result = HackerNewsParser.ParseCommentsPage(e.Result);
                };

                updateCommentsWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    List<Comment> comments = (List<Comment>)args.Result;

                    foreach (Comment c in comments)
                        CommentListBox.Items.Add(CommentUtils.UIElementFromComment(c));

                    loadingDialog.Hide();
                };

                updateCommentsWorker.RunWorkerAsync();
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
                return;

            // Fetch the comments for the story
            int postId = ((NewsItem)DataContext).Id;

            client.DownloadStringAsync(new Uri(MainPage.BaseUrls[0] + "/item?id=" + postId));

            loadingDialog = new LoadingProgressControl();
            loadingDialog.Show();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Navigating away from the page - cancel any outstanding connection attempts and hide the loading control
            client.CancelAsync();
            loadingDialog.Hide();
        }
    }
}