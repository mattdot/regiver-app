using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Web.Http;
using Windows.UI.Xaml.Media.Imaging;
using System.Net.Http;
using System.Threading.Tasks;

namespace Regiver.Data
{
    public class DataModel
    {
        private const string subscriptionKey = "2398b934ec9e40d09afecfdce2692a36";
        private const string mikesId = "123";

        private static DataModel current;

        public DataModel()
        {
            this.Cards = new ObservableCollection<GiftCard>();

            if (DesignMode.DesignModeEnabled)
            {
                this.Cards.Add(new GiftCard
                    {
                        Name = "Starbucks",
                        Balance = 2.45m,
                        Id = Guid.NewGuid().ToString()
                    });

                this.Cards.Add(new GiftCard
                {
                    Name = "Sears",
                    Balance = 10.95m,
                    Id = Guid.NewGuid().ToString()
                });

                this.Cards.Add(new GiftCard
                {
                    Name = "Microsoft Store",
                    Balance = 15.4m,
                    Id = Guid.NewGuid().ToString()
                });
            }
            else
            {
                this.GetData();
            }
        }

        public async Task<GiftCard> AddCardAsync(string cardNumber)
        {
            var client = new Windows.Web.Http.HttpClient();

            var uriString = string.Format(
                "https://regiver.azure-api.net/v1/accounts/{0}/cards?subscription-key={1}",
                mikesId,
                subscriptionKey);

            var uri = new Uri(uriString);

            var json = new JsonObject();

            json.Add("id", JsonValue.CreateStringValue(cardNumber));
            json.Add("type", JsonValue.CreateStringValue("starbucks"));
            json.Add("csc", JsonValue.CreateStringValue("1234"));

            var content = json.Stringify();

            var message = await client.PostAsync(uri, new HttpStringContent(content));

            var responseString = await message.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(responseString);

            JsonObject card = null;

            if (JsonObject.TryParse(responseString, out card))
            {
                var image = card.GetNamedString("image");

                var newCard = new GiftCard
                {
                    Id = card.GetNamedString("id"),
                    Type = card.GetNamedString("type"),
                    Name = card.GetNamedString("title"),
                    Image = new BitmapImage(new Uri(image)),
                    Balance = System.Convert.ToDecimal(card.GetNamedNumber("value")),
                };

                this.Cards.Add(newCard);

                return newCard;
            }

            return null;
        }

        private async void GetData()
        {
            var client = new Windows.Web.Http.HttpClient();

            var uriString = string.Format(
                CultureInfo.InvariantCulture,
                "https://regiver.azure-api.net/v1/accounts/{0}?subscription-key={1}",
                mikesId,
                subscriptionKey);

            var uri = new Uri(uriString);

            try
            {
                var json = await client.GetStringAsync(uri);

                System.Diagnostics.Debug.WriteLine(json);

                JsonObject accounts = null;

                if (Windows.Data.Json.JsonObject.TryParse(json, out accounts))
                {
                    var cards = from item in accounts.GetNamedArray("cards")
                                let card = item.GetObject()
                                let image = card.GetNamedString("image")
                                select new GiftCard
                                {
                                    Id = card.GetNamedString("id"),
                                    Type = card.GetNamedString("type"),
                                    Name = card.GetNamedString("title"),
                                    Image = new BitmapImage(new Uri(image)),
                                    Balance = System.Convert.ToDecimal(card.GetNamedNumber("value")),
                                };

                    foreach (var card in cards)
                    {
                        this.Cards.Add(card);
                    }

                }
            }
            catch (System.Exception se)
            {
                System.Diagnostics.Debug.WriteLine(se.Message);
            }
        }

        public static DataModel Current
        {
            get
            {
                if (current == null)
                {
                    current = new DataModel();
                }

                return current;
            }
        }

        public ObservableCollection<GiftCard> Cards { get; private set; }
    }
}
