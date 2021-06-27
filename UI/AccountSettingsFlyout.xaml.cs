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
using System.Diagnostics;
using System.Text;
using Kemunpus.Web2Mail.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kemunpus.Web2Mail.UI {

    public sealed class AccountSettings : Settings {

        public static readonly AccountSettings Current = new AccountSettings();

        private AccountSettings() {
        }

        public string From {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string To0 {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string To1 {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string To2 {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string To3 {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string Cc {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string SmtpServer {

            get {
                return GetValue("smtp.live.com");
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string SmtpId {

            get {
                return GetValue(string.Empty);
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string SmtpPassword {

            get {
                return GetPassword(resourceName: AppUtils.GetResourceString("AppName"), userName: SmtpId);
            }

            set {
                SetPassword(resourceName: AppUtils.GetResourceString("AppName"), userName: SmtpId, password: value);
            }
        }

        public int SmtpPort {

            get {
                return GetValue(587);
            }

            set {
                value = Math.Min(value, 65535);
                value = Math.Max(value, 1);

                SetValue(value);
            }
        }

        public Encoding SmtpCharset {

            get {
                return GetValue(Encoding.UTF8);
            }

            set {

                if (value != null) {
                    SetValue(value);
                }
            }
        }

        public bool UseSingleLineSubject {

            get {
                return GetValue(true);
            }

            set {
                SetValue(value);
            }
        }
    }

    public sealed partial class AccountSettingsFlyout : SettingsFlyout {

        public AccountSettingsFlyout() {
            InitializeComponent();

            DataContext = AccountSettings.Current;

            Debug.Assert(DataContext != null);

            bool hasLicense = App.HasLicense();

            To1.IsEnabled = To2.IsEnabled = To3.IsEnabled = Cc.IsEnabled = hasLicense;

            PurchaseButton.IsEnabled = !hasLicense;

            LicenseDescription.Text = AppUtils.GetResourceString(hasLicense ? "Message/Purchased" : "Message/NotPurchased");
        }

        private async void CheckButtonClick(object sender, RoutedEventArgs args) {
            CheckButton.IsEnabled = false;

            try {
                CheckResult.Text = AppUtils.GetResourceString("Message/CheckStart");

                SmtpClient smtpClient = new SmtpClient(server: AccountSettings.Current.SmtpServer, port: AccountSettings.Current.SmtpPort, id: AccountSettings.Current.SmtpId, password: AccountSettings.Current.SmtpPassword, encoding: AccountSettings.Current.SmtpCharset, useAttachment: OptionSettings.Current.UseAttachment, useSingleLineSubject: AccountSettings.Current.UseSingleLineSubject);

                await smtpClient.SendMailAsync(from: AccountSettings.Current.From, to: AccountSettings.Current.To0, cc: "", subject: "", body: null);

                CheckResult.Text = AppUtils.GetResourceString("Message/CheckOk");

            } catch (Exception e) {
                CheckResult.Text = string.Format("{0}{1}{2}", AppUtils.GetResourceString("Message/CheckNG"), Environment.NewLine, e.Message);
            }

            CheckButton.IsEnabled = true;
        }

        private async void PurchaseButtonClick(object sender, RoutedEventArgs args) {
            await AppUtils.RequestProductPurchaseAsync(App.ProductName);
        }
    }
}
