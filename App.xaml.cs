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
using System.Globalization;
using Kemunpus.Web2Mail.Common;
using Kemunpus.Web2Mail.UI;
using Windows.ApplicationModel.Activation;
using Windows.Data.Xml.Dom;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kemunpus.Web2Mail {

    public sealed partial class App : Application {

        public static readonly string ProductName = "Web2MailLicense";

        public App() {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args) {
            Frame frame = Window.Current.Content as Frame;

            if (frame == null) {
                frame = new Frame();

                Window.Current.Content = frame;

                UnhandledException += /* async */ (sender, unhandledExceptionEventArgs) => {
                    unhandledExceptionEventArgs.Handled = true; // !IsDeveloperMode();

                    Debug.WriteLine("App : un-handled exception [{0}] has occurred.", unhandledExceptionEventArgs.Exception.ToString());

                    // await AppUtils.ShowErrorMessageAsync("Message/InternalError", unhandledExceptionEventArgs.Exception);
                };

                SettingsPane.GetForCurrentView().CommandsRequested += (settingsPane, settingsPaneCommandRequestedEventArgs) => {

                    settingsPaneCommandRequestedEventArgs.Request.ApplicationCommands.Add(new SettingsCommand("AccountSettings", AppUtils.GetResourceString("UI/AccountSettingsCommand"), (command) => {
                        new AccountSettingsFlyout().ShowIndependent();
                    }));

                    settingsPaneCommandRequestedEventArgs.Request.ApplicationCommands.Add(new SettingsCommand("Help", AppUtils.GetResourceString("UI/HelpCommand"), async (command) => {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(AppUtils.GetResourceString("Config/HelpUri")));
                    }));

                    settingsPaneCommandRequestedEventArgs.Request.ApplicationCommands.Add(new SettingsCommand("PrivacyPolicy", AppUtils.GetResourceString("UI/PrivacyPolicyCommand"), async (command) => {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(AppUtils.GetResourceString("Config/PrivacyPolicyUri")));
                    }));
                };
            }

            string parameter = null;

            if (args != null) {
                parameter = args.Arguments;
            }

            if (string.IsNullOrEmpty(parameter)) {
                parameter = AppUtils.GetResourceString("Config/StartUri");
            }

            frame.Navigate(typeof(MainPage), parameter);

            Window.Current.Activate();
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args) {
            new SharePage().ActivateAsync(args);
        }

        public static bool IsAccountSettingsEnough() {
            return !(string.IsNullOrEmpty(AccountSettings.Current.From) || string.IsNullOrEmpty(AccountSettings.Current.To0) || string.IsNullOrEmpty(AccountSettings.Current.SmtpServer));
        }

        public static bool IsDeveloperMode() {
            return (AccountSettings.Current.From != null && AccountSettings.Current.From == "KEMUNPUS@HOTMAIL.COM" && AccountSettings.Current.SmtpId != null && AccountSettings.Current.SmtpId == "kemunpus@hotmail.com");
        }

        public static bool HasLicense() {
            return IsDeveloperMode() ? true : AppUtils.IsProductPurchased(ProductName);
        }

        public static void ShowToast(string uri, string to, string subject) {
            ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();

            if (toastNotifier != null && toastNotifier.Setting == NotificationSetting.Enabled) {
                XmlDocument content = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

                if (content != null) {
                    XmlAttribute launch = content.CreateAttribute("launch");

                    if (launch != null) {
                        launch.Value = uri;
                        content.GetElementsByTagName("toast")[0].Attributes.SetNamedItem(launch);
                        content.GetElementsByTagName("text")[0].AppendChild(content.CreateTextNode(string.Format(CultureInfo.CurrentCulture, AppUtils.GetResourceString("Message/ToastMessage"), to, subject)));

                        toastNotifier.Show(new ToastNotification(content));
                    }
                }
            }
        }
    }
}
