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
        private const string mikesId = "45415083d32d4901a41bab9120737269";

        private static DataModel current;

        public DataModel()
        {
            this.Cards = new ObservableCollection<GiftCard>();
            
            this.Charities = new ObservableCollection<Charity>();

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

                AddDefaultCharities();
            }
            else
            {
                this.GetData();
            }
        }

        public string SelectedCharityId { get; set; }

        public async Task<string> DonateAsync(string giftCardId, string charityId)
        {
            var client = new Windows.Web.Http.HttpClient();

            var uriString = string.Format(
                "https://regiver.azure-api.net/v1/accounts/{0}/donations?subscription-key={1}",
                mikesId,
                subscriptionKey);

            var uri = new Uri(uriString);

            var json = new JsonObject();

            json.Add("card_id", JsonValue.CreateStringValue(giftCardId));
            json.Add("charity_id", JsonValue.CreateStringValue(charityId));

            var content = json.Stringify();

            var stringContent = new HttpStringContent(content);

            stringContent.Headers.ContentType.MediaType = "application/JSON";

            var message = await client.PostAsync(uri, stringContent);

            var responseString = await message.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(responseString);

            JsonObject card = null;

            if (JsonObject.TryParse(responseString, out card))
            {
                return card.GetNamedString("message");
            }

            return null;
        }

        private void AddDefaultCharities()
        {
            this.Charities.Add(new Charity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Boys and Girls Club of America",
                Logo = new BitmapImage(new Uri("http://www.chrisdraftfamilyfoundation.org/tools/partners/files/0027.jpg"))
            });
            this.Charities.Add(new Charity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Wounded Warrior Project",
                Logo = new BitmapImage(new Uri("http://vogeltalksrving.com/wp-content/uploads/2011/09/Wounded_Warrior_Project_25.jpg"))
            });

            this.Charities.Add(new Charity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Bill and Melinda Gates Foundation",
                Logo = new BitmapImage(new Uri("http://blogs-images.forbes.com/mfonobongnsehe/files/2012/07/Bill-and-Melinda-Gates-Foundation1.png"))
            });

            this.Charities.Add(new Charity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "The Human Fund",
                Logo = new BitmapImage(new Uri("http://reganwolfrom.files.wordpress.com/2009/12/seinfeld_human_fund_blue_shirt.jpg"))
            });
        }

        public ObservableCollection<Charity> Charities { get; private set; }

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

            var stringContent = new HttpStringContent(content);

            stringContent.Headers.ContentType.MediaType = "application/JSON";

            var message = await client.PostAsync(uri, stringContent);

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
            try
            {
                await GetCardsAsync();

                await GetCharitiesAsync();
            }
            catch (System.Exception se)
            {
                System.Diagnostics.Debug.WriteLine(se.Message);
            }
        }

        private async Task GetCharitiesAsync()
        {
            using (var client = new Windows.Web.Http.HttpClient())
            {
                var uriString = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://regiver.azure-api.net/v1/charities?subscription-key={0}",
                    subscriptionKey);

                var uri = new Uri(uriString);
                var json = await client.GetStringAsync(uri);

                System.Diagnostics.Debug.WriteLine(json);

                JsonObject charities = null;

                if (Windows.Data.Json.JsonObject.TryParse(json, out charities))
                {
                    var items = from item in charities.GetNamedArray("charities")
                                let charity = item.GetObject()
                                let logo_path = charity.GetNamedString("logo_path")
                                select new Charity
                                {
                                    Id = charity.GetNamedString("organization_uuid"),
                                    Name = charity.GetNamedString("organization_name"),
                                    Logo = new BitmapImage(new Uri(logo_path)),
                                    Location = string.Format("{0}, {1}",
                                                charity.GetNamedString("city"),
                                                charity.GetNamedString("region"))
                                };

                    foreach (var item in items)
                    {
                        this.Charities.Add(item);
                    }

                }
            }
        }

        private async Task GetCardsAsync()
        {
            using (var client = new Windows.Web.Http.HttpClient())
            {
                var uriString = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://regiver.azure-api.net/v1/accounts/{0}?subscription-key={1}",
                    mikesId,
                    subscriptionKey);

                var uri = new Uri(uriString);
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
