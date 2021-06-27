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
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;

namespace Kemunpus.Web2Mail.Common {

    public sealed class AppUtils {

        private static readonly ResourceLoader ResourceLoader = ResourceLoader.GetForViewIndependentUse();

        private AppUtils() {
            Debug.Assert(ResourceLoader != null);
        }

        public static bool IsProductPurchased(string productName) {
            Debug.Assert(!string.IsNullOrEmpty(productName));

            LicenseInformation licenseInformation = CurrentApp.LicenseInformation;

            if (licenseInformation != null && licenseInformation.ProductLicenses[productName].IsActive) {
                Debug.WriteLine("AppUtils : license for [{0}] is enabled.", productName);

                return true;
            }

            Debug.WriteLine("AppUtils : license for [{0}] is disabled.", productName);

            return false;
        }

        public async static Task RequestProductPurchaseAsync(string productName) {
            Debug.Assert(!string.IsNullOrEmpty(productName));

            try {
                await CurrentApp.RequestProductPurchaseAsync(productName);

            } catch (Exception e) {
                Debug.WriteLine("AppUtils : exception [{0}] has occurred when request purchase the product [{1}].", e.ToString(), productName);
            }
        }

        public static async Task ShowErrorMessageAsync(string resourceKey, Exception exception = null) {
            Debug.Assert(!string.IsNullOrEmpty(resourceKey));

            await new MessageDialog(CreateErrorMessage(resourceKey, exception)).ShowAsync();
        }

        public static string CreateErrorMessage(string resourceKey, Exception exception = null) {
            Debug.Assert(!string.IsNullOrEmpty(resourceKey));

            StringBuilder buffer = new StringBuilder(GetResourceString(resourceKey));

            if (exception != null) {

                if (!string.IsNullOrEmpty(exception.Message)) {
                    buffer.Append(Environment.NewLine);
                    buffer.Append(exception.Message);
                }

                Debug.WriteLine(exception.StackTrace);
            }

            string errorMessage = buffer.ToString();

            Debug.WriteLine(errorMessage);

            return errorMessage;
        }

        public static string GetResourceString(string key) {
            Debug.Assert(!string.IsNullOrEmpty(key));

            string value = ResourceLoader.GetString(key);

            if (value == null) {
                throw new ArgumentException(key);
            }

            return value;
        }
    }
}
