/*
 * Web2Mail Version 1.1
 * 
 * Copyright 2014 kemunpus@hotmail.com.
 * 
 * Project Page:
 * https://kemunpus.visualstudio.com/DefaultCollection/KemunpusProject
 * 
 * See also:
 * http://kemunpus.azurewebsites.net/
 */
using Kemunpus.Web2Mail.Common;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Kemunpus.Web2Mail.UI {

    public sealed partial class MainPage : Page {

        public MainPage() {
            InitializeComponent();

            DataContext = new Session(toComboBox: ToComboBox, sendButton: SendButton, optionSettingsButton: OptionSettingsButton, loadButton: LoadButton, previewWebView: PreviewWebView, previewTextBox: PreviewTextBox, progressRing: ProgressRing);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args) {
            base.OnNavigatedTo(args);

            if (!App.IsAccountSettingsEnough()) {
                await AppUtils.ShowErrorMessageAsync("Message/InsufficientAccountSettings");

                new AccountSettingsFlyout().ShowIndependent();

                return;
            }

            if (args != null) {
                string url = args.Parameter as string;

                if (!string.IsNullOrEmpty(url)) {
                    Session session = DataContext as Session;

                    if (session != null) {
                        session.Url = url;

                        await session.ExecuteOperationAsync(Session.Operation.LoadWeb);
                    }
                }
            }
        }

        private async void SendButtonClick(object sender, RoutedEventArgs args) {
            Session session = DataContext as Session;

            if (session != null) {
                session.To = (string)ToComboBox.SelectedItem;

                await session.ExecuteOperationAsync(Session.Operation.SendMail);
            }
        }

        private void OptionSettingsButtonClick(object sender, RoutedEventArgs args) {
            OptionSettingsFlyout optionSettingsFlyout = new OptionSettingsFlyout();

            optionSettingsFlyout.Unloaded += async (object s, RoutedEventArgs a) => {
                Session session = DataContext as Session;

                if (session != null) {
                    await session.ExecuteOperationAsync(Session.Operation.LoadWeb);
                }
            };

            optionSettingsFlyout.Show();
        }

        private async void LoadButtonClick(object sender, RoutedEventArgs args) {
            Session session = DataContext as Session;

            if (session != null) {
                await session.ExecuteOperationAsync(Session.Operation.LoadWeb);
            }
        }

        private async void SubjectKeyDown(object sender, KeyRoutedEventArgs args) {

            if (args != null && args.Key == VirtualKey.Enter) {
                args.Handled = true;

                Session session = DataContext as Session;

                if (session != null) {
                    session.To = (string)ToComboBox.SelectedItem;

                    await session.ExecuteOperationAsync(Session.Operation.SendMail);
                }
            }
        }

        private async void UrlKeyDown(object sender, KeyRoutedEventArgs args) {

            if (args != null && args.Key == VirtualKey.Enter) {
                args.Handled = true;

                Session session = DataContext as Session;

                if (session != null) {
                    session.Url = Url.Text;

                    await session.ExecuteOperationAsync(Session.Operation.LoadWeb);
                }
            }
        }

        private void PreviewNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {

            if (args != null && args.Uri != null) {
                args.Cancel = true;
            }
        }

        private void PreviewNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args) {

            if (args != null && args.IsSuccess && sender != null && sender.DocumentTitle != null) {
                Session session = DataContext as Session;

                if (session != null) {
                    session.Subject = sender.DocumentTitle;
                }
            }
        }

        private void MainPageGotFocus(object sender, RoutedEventArgs args) {
            Session session = DataContext as Session;

            if (session != null) {
                session.ApplyAccountSettings();
            }
        }

        private void UrlGotFocus(object sender, RoutedEventArgs args) {
            Url.SelectAll();
        }

        private void PreviewGotFocus(object sender, RoutedEventArgs args) {
            Focus(FocusState.Programmatic);
        }
    }
}
