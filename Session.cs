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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Kemunpus.Web2Mail.Common;
using Kemunpus.Web2Mail.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kemunpus.Web2Mail {

    public sealed class Session : INotifyPropertyChanged {

        public enum Operation {
            LoadWeb,
            SendMail
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _Url = string.Empty;
        private string _Subject = string.Empty;
        private string _To = string.Empty;
        private string _Content = string.Empty;

        private readonly ComboBox ToComboBox;
        private readonly AppBarButton SendButton;
        private readonly AppBarButton OptionSettingsButton;
        private readonly AppBarButton LoadButton;
        private readonly WebView PreviewWebView;
        private readonly TextBox PreviewTextBox;
        private readonly ProgressRing ProgressRing;

        public Session(ComboBox toComboBox, AppBarButton sendButton, AppBarButton optionSettingsButton, AppBarButton loadButton, WebView previewWebView, TextBox previewTextBox, ProgressRing progressRing) {
            ToComboBox = toComboBox;
            SendButton = sendButton;
            OptionSettingsButton = optionSettingsButton;
            LoadButton = loadButton;
            PreviewWebView = previewWebView;
            PreviewTextBox = previewTextBox;
            ProgressRing = progressRing;
        }

        public string Url {

            get {
                return _Url;
            }

            set {

                if (value != null) {
                    _Url = value.Trim();

                    NotifyPropertyChanged(value);
                }
            }
        }

        public string Subject {

            get {
                return _Subject;
            }

            set {

                if (value != null) {
                    _Subject = value.Trim();

                    NotifyPropertyChanged(value);
                }
            }
        }

        public string To {

            get {
                return _To;
            }

            set {

                if (value != null) {
                    _To = value.Trim();
                }
            }
        }

        public string Content {

            get {
                return _Content;
            }

            set {

                if (value != null) {
                    _Content = value;
                }
            }
        }

        public void ApplyAccountSettings() {
            int selectedIndex = ToComboBox.SelectedIndex;
            int itemsCount = ToComboBox.Items.Count;

            List<string> newList = new List<string>(4);

            if (!string.IsNullOrEmpty(AccountSettings.Current.To0)) {
                newList.Add(AccountSettings.Current.To0);
            }

            if (!string.IsNullOrEmpty(AccountSettings.Current.To1)) {
                newList.Add(AccountSettings.Current.To1);
            }

            if (!string.IsNullOrEmpty(AccountSettings.Current.To2)) {
                newList.Add(AccountSettings.Current.To2);
            }

            if (!string.IsNullOrEmpty(AccountSettings.Current.To3)) {
                newList.Add(AccountSettings.Current.To3);
            }

            ToComboBox.DataContext = newList;

            if (itemsCount != newList.Count) {
                selectedIndex = 0;
            }

            ToComboBox.SelectedIndex = selectedIndex;
        }

        public async Task ExecuteOperationAsync(Operation operation) {
            Exception exception = null;

            string errorMessage = "Message/InternalError";

            ProgressRing.IsActive = true;

            SendButton.IsEnabled = OptionSettingsButton.IsEnabled = LoadButton.IsEnabled = false;

            ToComboBox.Focus(FocusState.Programmatic);

            try {

                switch (operation) {

                    case Operation.LoadWeb:
                        errorMessage = "Message/WebError";

                        await LoadWebsiteAsync();

                        break;

                    case Operation.SendMail:
                        errorMessage = "Message/MailError";

                        await SendMailAsync();

                        break;
                }

            } catch (Exception e) {
                exception = e;
            }

            ProgressRing.IsActive = false;

            SendButton.IsEnabled = OptionSettingsButton.IsEnabled = LoadButton.IsEnabled = true;

            if (exception != null) {
                await AppUtils.ShowErrorMessageAsync(errorMessage, exception);
            }
        }

        public async Task LoadWebsiteAsync() {
            Content = string.Empty;

            PreviewWebView.NavigateToString(Content);

            PreviewTextBox.Text = Content;

            if (!string.IsNullOrEmpty(Url)) {

                if (Url.IndexOf("://") < 0) {
                    Url = string.Format("http://{0}", Url);
                }

                using (WebClient webClient = new WebClient(httpCharSet: OptionSettings.Current.HttpCharset, maxAutomaticRedirections: OptionSettings.Current.HttpMaxAutomaticRedirections, httpBufferSize: OptionSettings.Current.HttpBufferSize, timeout: OptionSettings.Current.HttpTimeout, htmlBufferSize: OptionSettings.Current.HtmlBufferSize, userAgent: OptionSettings.Current.HttpUserAgent, acceptLanguage: OptionSettings.Current.HttpAcceptLanguage)) {
                    Uri uri = new Uri(Url);

                    Content = await webClient.LoadHtmlAsync(uri);

                    Content = webClient.NormalizeLink(Content, uri);

                    if (OptionSettings.Current.RemoveScript) {
                        Content = webClient.RemoveScript(Content);
                    }

                    if (OptionSettings.Current.RemoveNop) {
                        Content = webClient.RemoveNop(Content);
                    }

                    if (OptionSettings.Current.UseAttachment) {
                        Content = await webClient.ConvertLinkToAttachmentAsync(Content);
                    }
                }
            }

            if (OptionSettings.Current.UseWebView) {
                PreviewWebView.Visibility = Visibility.Visible;
                PreviewTextBox.Visibility = Visibility.Collapsed;

                PreviewWebView.NavigateToString(Content);

            } else {
                PreviewWebView.Visibility = Visibility.Collapsed;
                PreviewTextBox.Visibility = Visibility.Visible;

                PreviewTextBox.Text = Content;
            }
        }

        public async Task SendMailAsync() {

            using (SmtpClient smtpClient = new SmtpClient(server: AccountSettings.Current.SmtpServer, port: AccountSettings.Current.SmtpPort, id: AccountSettings.Current.SmtpId, password: AccountSettings.Current.SmtpPassword, encoding: AccountSettings.Current.SmtpCharset, useAttachment: OptionSettings.Current.UseAttachment, useSingleLineSubject: AccountSettings.Current.UseSingleLineSubject)) {
                await smtpClient.SendMailAsync(from: AccountSettings.Current.From, to: To, cc: AccountSettings.Current.Cc, subject: Subject, body: Content);
            }
        }

        private void NotifyPropertyChanged(string value, [CallerMemberName] string key = "") {
            PropertyChanged(this, new PropertyChangedEventArgs(key));
        }
    }
}