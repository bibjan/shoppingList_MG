using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using shoppingList.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shoppingList.ViewModels
{
    public class ShopsViewModel : ObservableObject
    {
        public IAsyncRelayCommand NewShopCommand { get; set; }
        public ObservableCollection<ShopItemViewModel> Shops { get; } = new();
        public static ShopsViewModel Instance { get; } = new();

        public ShopsViewModel()
        {
            NewShopCommand = new AsyncRelayCommand(NewShopAsync);
        }

        private async Task NewShopAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("Nowy sklep",
                "Podaj nazwę sklepu:",
                "OK",
                "Anuluj");
            var shopName = input?.Trim();

            if (string.IsNullOrWhiteSpace(shopName))
            {
                return;
            }

            Shops.Add(new ShopItemViewModel(shopName));

            Data.Save();
        }
    }
}
