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
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kemunpus.Web2Mail.Common;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Kemunpus.Web2Mail.UI {

    public sealed partial class SharePage : Page {

        private ShareOperation shareOperation;

        public SharePage() {
            InitializeComponent();

            DataContext = new Session(toComboBox: ToComboBox, sendButton: SendButton, optionSettingsButton: OptionSettingsButton, loadButton: LoadButton, previewWebView: PreviewWebView, previewTextBox: PreviewTextBox, progressRing: ProgressRing);
        }

        public async void ActivateAsync(ShareTargetActivatedEventArgs args) {
            Window.Current.Content = this;
            Window.Current.Activate();

            if (!App.IsAccountSettingsEnough()) {
                await AppUtils.ShowErrorMessageAsync("Message/InsufficientAccountSettings");

                shareOperation.DismissUI();

                return;
            }

            Session session = DataContext as Session;

            if (args == null || session == null) {
                shareOperation.DismissUI();

                return;
            }

            shareOperation = args.ShareOperation;

            if (shareOperation.Data.Contains(StandardDataFormats.WebLink)) {
                session.Url = (await shareOperation.Data.GetWebLinkAsync()).AbsoluteUri;

                if (shareOperation.Data.Properties != null && !string.IsNullOrEmpty(shareOperation.Data.Properties.Title)) {
                    session.Subject = shareOperation.Data.Properties.Title;
                }

            } else if (shareOperation.Data.Contains(StandardDataFormats.Text)) {
                string text = await shareOperation.Data.GetTextAsync();

                if (!string.IsNullOrEmpty(text)) {
                    session.Subject = text;

                    Match match = new Regex("http(s)?://([\\w-]+\\.)+[\\w-]+(/[\\w-./?%&=]*)?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Match(text);

                    if (match.Success) {
                        session.Url = match.Value;
                    }
                }
            }

            await session.ExecuteOperationAsync(Session.Operation.LoadWeb);
        }

        private new void GotFocus(object sender, RoutedEventArgs args) {
            Session session = DataContext as Session;

            if (session != null) {
                session.ApplyAccountSettings();
            }
        }

        private async new void KeyDown(object sender, KeyRoutedEventArgs args) {

            if (args != null && args.Key == VirtualKey.Enter) {
                args.Handled = true;

                await ExecuteShareOperationAsync();
            }
        }

        private async void SendButtonClick(object sender, RoutedEventArgs args) {
            await ExecuteShareOperationAsync();
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

        private void PreviewNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {

            if (args != null && args.Uri != null) {
                args.Cancel = true;
            }
        }

        private async void PreviewDOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {

            if (OptionSettings.Current.UseZoomOut) {
                await sender.InvokeScriptAsync("eval", new string[] { "(function (){ document.body.style.zoom=0.5; })();" });
            }
        }

        private void PreviewGotFocus(object sender, RoutedEventArgs args) {
            Focus(FocusState.Programmatic);
        }

        private async Task ExecuteShareOperationAsync() {

            try {
                shareOperation.ReportStarted();

                SendButton.IsEnabled = OptionSettingsButton.IsEnabled = LoadButton.IsEnabled = false;
                ProgressRing.IsActive = true;
                Window.Current.Content.Opacity = 0.5;

                shareOperation.DismissUI();

                Session session = DataContext as Session;

                if (session != null) {
                    session.To = ToComboBox.SelectedItem as string;

                    shareOperation.ReportDataRetrieved();

                    await session.SendMailAsync();

                    App.ShowToast(uri: session.Url, to: session.To, subject: session.Subject);
                }

                shareOperation.ReportCompleted();

            } catch (Exception e) {
                shareOperation.ReportError(AppUtils.CreateErrorMessage("Message/ShareError", e));
            }
        }
    }
}
