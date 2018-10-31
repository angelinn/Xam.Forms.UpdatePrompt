﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xam.Plugin.AutoUpdate.Exceptions;
using Xam.Plugin.AutoUpdate.Services;
using Xamarin.Forms;

namespace Xam.Plugin.AutoUpdate
{
    public class UpdateManager
    {
        private readonly string title;
        private readonly string message;
        private readonly string confirm;
        private readonly string cancel;
        private readonly Func<Task<UpdatesCheckResponse>> checkForUpdatesFunction;
        private readonly TimeSpan? runEvery;

        private bool didCheck;
        private readonly Page mainPage;
        
        public UpdateManager(string title, string message, string confirm, string cancel, Func<Task<UpdatesCheckResponse>> checkForUpdatesFunction, TimeSpan? runEvery = null)
        {
            this.title = title;
            this.message = message;
            this.confirm = confirm;
            this.cancel = cancel;
            this.runEvery = runEvery;
            this.checkForUpdatesFunction = checkForUpdatesFunction ?? throw new AutoUpdateException("Check for updates function not provided. You must pass it in the constructor.");

            mainPage = Application.Current.MainPage;
            mainPage.Appearing += OnMainPageAppearing;
        }

        public UpdateManager(UpdateManagerParameters parameters) 
            : this (parameters.Title, parameters.Message, parameters.Confirm, parameters.Cancel, parameters.CheckForUpdatesFunction, parameters.RunEvery)
        {

        }

        private async void OnMainPageAppearing(object sender, EventArgs e)
        {
            if (!didCheck)
            {
                didCheck = true;

                if (runEvery.HasValue)
                {
                    DateTime lastUpdateTime = (DateTime)Application.Current.Properties["UpdateManager.LastUpdateTime"];
                    if (lastUpdateTime + runEvery.Value > DateTime.Now)
                    {
                        Application.Current.Properties["UpdateManager.LastUpdateTime"] = DateTime.Now;
                        await CheckForUpdatesAsync();
                    }
                }
                else
                    await CheckForUpdatesAsync();
            }
        }
        
        private async Task CheckForUpdatesAsync()
        {
            UpdatesCheckResponse response = await checkForUpdatesFunction();
            if (response.IsNewVersionAvailable)
            {
                if (await mainPage.DisplayAlert(title, message, confirm, cancel))
                {
                    HttpResponseMessage httpResponse = await new HttpClient().GetAsync(response.DownloadUrl);
                    byte[] data = await httpResponse.Content.ReadAsByteArrayAsync();

                    string fileName = response.DownloadUrl.Substring(response.DownloadUrl.LastIndexOf("/") + 1);
                    DependencyService.Get<IFileOpener>().OpenFile(data, fileName);
                }
            }
        }
    }
}
