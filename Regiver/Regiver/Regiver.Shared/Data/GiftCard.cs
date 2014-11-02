using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Regiver.Data
{
    public class GiftCard
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public decimal Balance { get; set; }

        public string CardNumber { get; set; }

        public ImageSource Image { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }
    }
}
