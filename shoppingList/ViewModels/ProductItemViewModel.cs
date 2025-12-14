using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using shoppingList.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace shoppingList.ViewModels
{
    public class ProductItemViewModel : ObservableObject
    {
        private Product _product;

        public string? Name
        {
            get => _product.Name;
            set
            {
                if (_product.Name != value)
                {
                    _product.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOptional
        {
            get => _product.IsOptional;
            set
            {
                if (_product.IsOptional != value)
                {
                    _product.IsOptional = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Value
        {
            get => _product.Value;
            set
            {
                var newValue = value < 0 ? 0 : value;
                if (_product.Value != newValue)
                {
                    _product.Value = newValue;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsChecked
        {
            get => _product.IsChecked;
            set
            {
                if (_product.IsChecked != value)
                {
                    _product.IsChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? SelectedShop
        {
            get => _product.Shop;
            set
            {
                if (_product.Shop != value)
                {
                    _product.Shop = value;
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
            get => _product.Unit;
            set
            {
                if (_product.Unit != value)
                {
                    _product.Unit = value;
                    OnPropertyChanged();
                    ShoppingList.SaveFromViewModels(
                        ShoppingViewModel.Instance,
                        ShopsViewModel.Instance,
                        RecipesViewModel.Instance);
                }
            }
        }

        private readonly string[] _options = { "Opcjonalność", "Sklep" };

        public ProductItemViewModel(Product product)
        {
            _product = product;
            if (string.IsNullOrWhiteSpace(_product.Name))
                _product.Name = "Produkt";

            AddCommand = new RelayCommand(() =>
            {
                _product.Add();
                OnPropertyChanged(nameof(Value));
            });

            SubtractCommand = new RelayCommand(() =>
            {
                _product.Subtract();
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
                    _options);

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

                    if (!string.IsNullOrWhiteSpace(optional) && optional != "Anuluj")
                    {
                        IsOptional = !IsOptional;
                    }
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

                ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
            });
        }
    }
}