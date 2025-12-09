using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using shoppingList.Models;
using System;
using System.Collections.ObjectModel;

namespace shoppingList.ViewModels
{
    public class ProductItemViewModel : ObservableObject
    {
        private Shopping _shopping;

        public string? Name
        {
            get => _shopping.Name;
            set
            {
                if (_shopping.Name != value)
                {
                    _shopping.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOptional
        {
            get => _shopping.IsOptional;
            set
            {
                if (_shopping.IsOptional != value)
                {
                    _shopping.IsOptional = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Value
        {
            get => _shopping.Value;
            set
            {
                var newValue = value < 0 ? 0 : value;
                if (_shopping.Value != newValue)
                {
                    _shopping.Value = newValue;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsChecked
        {
            get => _shopping.IsChecked;
            set
            {
                if (_shopping.IsChecked != value)
                {
                    _shopping.IsChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? SelectedShop
        {
            get => _shopping.Shop;
            set
            {
                if (_shopping.Shop != value)
                {
                    _shopping.Shop = value;
                    OnPropertyChanged();
                }
            }
        }

        public IRelayCommand AddCommand { get; }
        public IRelayCommand SubtractCommand { get; }
        public IRelayCommand EditProductCommand { get; }

        public ObservableCollection<string> Units { get; } = new()
        {
            "szt.", "l", "kg", "ml", "g", "opak.", "szczypta", "łyzki"
        };

        public string? SelectedUnit
        {
            get => _shopping.Unit;
            set
            {
                if (_shopping.Unit != value)
                {
                    _shopping.Unit = value;
                    OnPropertyChanged();
                    Data.Save();
                }
            }
        }

        private string[] options =
        [
            "Opcjonalność", "Sklep"
        ];

        public ProductItemViewModel(Shopping shopping)
        {
            _shopping = shopping;
            if (string.IsNullOrWhiteSpace(_shopping.Name))
                _shopping.Name = "Produkt";

            OnPropertyChanged(nameof(Name));

            AddCommand = new RelayCommand(() =>
            {
                _shopping.Add();
                OnPropertyChanged(nameof(Value));
            });

            SubtractCommand = new RelayCommand(() =>
            {
                _shopping.Subtract();
                OnPropertyChanged(nameof(Value));
            });

            EditProductCommand = new RelayCommand(async () =>
            {
                var shopsViewModel = ShopsViewModel.Instance;
                var shopNames = shopsViewModel.Shops.Select(s => s.ShopName).ToArray();

                var selected = await Shell.Current.DisplayActionSheet(
                    "Co chcesz edytować?",
                    "Anuluj",
                    null,
                    options);

                if (string.IsNullOrWhiteSpace(selected) || selected == "Anuluj")
                {
                    return;
                }

                if (selected == "Opcjonalność")
                {
                    var optional = await Shell.Current.DisplayActionSheet(
                        "Wybierz opcjonalność",
                        "Anuluj",
                        null,
                        IsOptional ? "Nieopcjonalny" : "Opcjonalny");

                    IsOptional = !IsOptional;
                    OnPropertyChanged(nameof(IsOptional));
                }
                else if (selected == "Sklep")
                {
                    var selectedShopName = await Shell.Current.DisplayActionSheet(
                        "Wybierz sklep",
                        "Anuluj",
                        null,
                        shopNames);

                    if (string.IsNullOrWhiteSpace(selectedShopName) || selectedShopName == "Anuluj")
                    {
                        return;
                    }

                    var targetShop = shopsViewModel.Shops.First(s => s.ShopName == selectedShopName);
                    if (!targetShop.Products.Contains(this))
                    {
                        targetShop.Products.Add(this);
                    }

                    SelectedShop = selectedShopName;
                }
                Data.Save();
            });
        }
    }
}
