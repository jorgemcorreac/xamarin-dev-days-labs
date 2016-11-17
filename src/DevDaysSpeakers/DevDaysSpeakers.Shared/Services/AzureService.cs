using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DevDaysSpeakers.Model;
using DevDaysSpeakers.Services;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json;
using Xamarin.Forms;

[assembly: Dependency(typeof(AzureService))]

namespace DevDaysSpeakers.Services
{
    public class AzureService
    {
        private IMobileServiceSyncTable<Speaker> table;
        public MobileServiceClient Client { get; set; }

        public async Task Initialize()
        {
            if (Client?.SyncContext?.IsInitialized ?? false)
                return;

            var appUrl = "https://montemagnospeakers.azurewebsites.net";

            //Create our client
            Client = new MobileServiceClient(appUrl);

            //InitialzeDatabase for path
            var path = "syncstore.db";
            path = Path.Combine(MobileServiceClient.DefaultDatabasePath, path);

            //setup our local sqlite store and intialize our table
            var store = new MobileServiceSQLiteStore(path);

            //Define table
            store.DefineTable<Speaker>();

            //Initialize SyncContext
            await Client.SyncContext.InitializeAsync(store, new MobileServiceSyncHandler());

            //Get our sync table that will call out to azure
            table = Client.GetSyncTable<Speaker>();
        }

        public async Task<IEnumerable<Speaker>> GetSpeakers()
        {
            await Initialize();
            await SyncSpeakers();

            var me = await SyncMeLikeSpeaker();

            var speakers = new List<Speaker> {me};

            speakers.AddRange(await table.OrderBy(s => s.Name).ToEnumerableAsync());

            return speakers.AsEnumerable();
        }

        public async Task SyncSpeakers()
        {
            try
            {
                await Client.SyncContext.PushAsync();
                await table.PullAsync("allSpeakers", table.CreateQuery());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to sync speakers, that is alright as we have offline capabilities: " + ex);
            }
        }

        public async Task<Speaker> SyncMeLikeSpeaker()
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync("https://onedrive.live.com/download?cid=EBB2E10E506D08B6&resid=EBB2E10E506D08B6%21842497&authkey=ANTqSEcNnbjoxRE");

                var me = JsonConvert.DeserializeObject<Speaker>(json);

                return me;
            }
        }
    }
}