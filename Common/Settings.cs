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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Kemunpus.Web2Mail.Common {

    public abstract class Settings : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        private static readonly PasswordVault PasswordVault = new PasswordVault();

        protected Encoding GetValue(Encoding defaultValue, [CallerMemberName] string key = "") {
            Debug.Assert(key != null);

            if (defaultValue != null) {

                try {
                    var savedValue = LocalSettings.Values[key];
                    var value = savedValue == null ? defaultValue.WebName : savedValue;

                    return Encoding.GetEncoding(value.ToString());

                } catch (ArgumentException e) {
                    Debug.WriteLine("Settings : exception [{0}] has occurred when convert the value to encoding object.", e.ToString());
                }
            }

            return defaultValue;
        }

        protected void SetValue(Encoding value, [CallerMemberName] string key = "") {
            Debug.Assert(key != null);

            if (value != null) {
                LocalSettings.Values[key] = value.WebName;

                PropertyChanged(this, new PropertyChangedEventArgs(key));
            }
        }

        protected T GetValue<T>(T defaultValue, [CallerMemberName] string key = "") {
            Debug.Assert(key != null);

            var savedValue = LocalSettings.Values[key];
            var value = savedValue == null ? defaultValue : savedValue;

            return (T)value;
        }

        protected void SetValue<T>(T value, [CallerMemberName] string key = "") {
            Debug.Assert(key != null);

            LocalSettings.Values[key] = value;

            try {
                PropertyChanged(this, new PropertyChangedEventArgs(key));

            } catch (Exception e) {
                Debug.WriteLine("Settings : exception [{0}] has occurred when notify property changed.", e.ToString());
            }
        }

        protected string GetPassword(string resourceName, string userName) {
            Debug.Assert(!string.IsNullOrEmpty(resourceName));

            if (!string.IsNullOrEmpty(userName)) {

                try {
                    return PasswordVault.Retrieve(resourceName, userName).Password;

                } catch (Exception e) {
                    Debug.WriteLine("Settings : exception [{0}] has occurred when retrieving password from the password vault.", e.ToString());
                }
            }

            return string.Empty;
        }

        protected void SetPassword(string resourceName, string userName, string password) {
            Debug.Assert(!string.IsNullOrEmpty(resourceName));

            if (!string.IsNullOrEmpty(userName)) {

                try {
                    PasswordVault.Add(new PasswordCredential(resourceName, userName, password));

                } catch (Exception e) {
                    Debug.WriteLine("Settings : exception [{0}] has occurred when adding password to the password vault.", e.ToString());
                }
            }
        }
    }
}
