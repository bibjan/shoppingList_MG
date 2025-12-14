using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shoppingList.Models;


namespace shoppingList.ViewModels
{
    public class ShopItemViewModel : ObservableObject
    {
        public string ShopName { get; }
        public ObservableCollection<ProductItemViewModel> Products { get; } = new();


        public ShopItemViewModel(Shop shop)
        {
            ShopName = shop.Name;

            foreach (var p in shop.Products)
            {
                Products.Add(new ProductItemViewModel(p));
            }
        }
    }
}
