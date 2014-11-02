using Regiver.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Regiver.ViewModel
{
    public class MainPageViewModel
    {
        public ObservableCollection<GiftCard> Cards
        {
            get
            {
                return DataModel.Current.Cards;
            }
        }
    }
}
