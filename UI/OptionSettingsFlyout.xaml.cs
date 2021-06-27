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
using System.Text;
using Kemunpus.Web2Mail.Common;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

namespace Kemunpus.Web2Mail.UI {

    public sealed class OptionSettings : Settings {

        public static readonly OptionSettings Current = new OptionSettings();

        private OptionSettings() {
        }

        public int HtmlBufferSize {

            get {
                return GetValue(1024 * 1024 / 2);
            }

            set {
                value = Math.Min(value, 1024 * 1024 * 4);
                value = Math.Max(value, 1024 * 1024 / 2);

                SetValue(value);
            }
        }

        public int HttpBufferSize {

            get {
                return GetValue(1024 * 1024 / 2);
            }

            set {
                value = Math.Min(value, 1024 * 1024 * 4);
                value = Math.Max(value, 1024 * 1024 / 2);

                SetValue(value);
            }
        }

        public int HttpTimeout {

            get {
                return GetValue(30);
            }

            set {
                value = Math.Min(value, 360);
                value = Math.Max(value, 1);

                SetValue(value);
            }
        }

        public Encoding HttpCharset {

            get {
                return GetValue(Encoding.UTF8);
            }

            set {
                SetValue(value);
            }
        }

        public string HttpUserAgent {

            get {
                return GetValue("Mozilla/5.0 (compatible; Web2Mail/1.1)");
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public string HttpAcceptLanguage {

            get {
                return GetValue(AppUtils.GetResourceString("Config/HttpAcceptLanguage"));
            }

            set {

                if (value != null) {
                    SetValue(value.Trim());
                }
            }
        }

        public int HttpMaxAutomaticRedirections {

            get {
                return GetValue(8);
            }

            set {
                value = Math.Min(value, 64);
                value = Math.Max(value, 0);

                SetValue(value);
            }
        }

        public bool UseAttachment {

            get {
                return GetValue(false);
            }

            set {
                SetValue(value);
            }
        }

        public bool UseWebView {

            get {
                return GetValue(true);
            }

            set {
                SetValue(value);
            }
        }

        public bool UseZoomOut {

            get {
                return GetValue(false);
            }

            set {
                SetValue(value);
            }
        }

        public bool RemoveScript {

            get {
                return GetValue(true);
            }

            set {
                SetValue(value);
            }
        }

        public bool RemoveNop {

            get {
                return GetValue(true);
            }

            set {
                SetValue(value);
            }
        }
    }

    public sealed partial class OptionSettingsFlyout : SettingsFlyout {

        public OptionSettingsFlyout() {
            InitializeComponent();

            DataContext = OptionSettings.Current;

            Debug.Assert(DataContext != null);

            HtmlBufferSize.IsEnabled = HttpBufferSize.IsEnabled = App.HasLicense();

            if (App.IsDeveloperMode()) {
                VersionInfo.Text = string.Format(CultureInfo.CurrentCulture, "Developer Mode {0}.{1}.{2}.{3}", Package.Current.Id.Version.Major.ToString(CultureInfo.CurrentCulture), Package.Current.Id.Version.Minor.ToString(CultureInfo.CurrentCulture), Package.Current.Id.Version.Build.ToString(CultureInfo.CurrentCulture), Package.Current.Id.Version.Revision.ToString(CultureInfo.CurrentCulture));

            } else {
                VersionInfo.Text = string.Empty;
            }
        }

        private new void BackClick(object sender, BackClickEventArgs e) {

            if (e != null) {
                e.Handled = true;

                Hide();
            }
        }
    }
}
