using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using shoppingList.Models;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;

namespace shoppingList.ViewModels
{
    public class RecipeItemViewModel : ObservableObject
    {
        public string RecipeName { get; }
        public ObservableCollection<string> Steps { get; } = new();

        public IAsyncRelayCommand ImportToListCommand { get; }
        public ObservableCollection<ProductItemViewModel> Products { get; } = new();

        public RecipeItemViewModel(string recipeName, IEnumerable<string>? steps = null)
        {
            RecipeName = recipeName;
            if (steps != null)
            {
                foreach (var s in steps)
                    Steps.Add(s);
            }

            Steps.CollectionChanged += Steps_CollectionChanged;
            ImportToListCommand = new AsyncRelayCommand(ImportToListAsync);
        }

        private void Steps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(StepsDisplay));
            OnPropertyChanged(nameof(StepLines));
        }

        public string StepsDisplay => string.Join(Environment.NewLine, Steps.Select((s, i) => $"{i + 1}. {s}"));

        public IEnumerable<string> StepLines => Steps.Select((s, i) => $"{i + 1}. {s}");

        private async Task ImportToListAsync()
        {
            var shoppingViewModel = ShoppingViewModel.Instance;

            if (shoppingViewModel.Categories.Count == 0)
            {
                await Shell.Current.DisplayAlert("Błąd", "Brak kategorii. Najpierw dodaj kategorię.", "OK");
                return;
            }

            foreach (var product in Products.ToList())
            {
                var categoryNames = shoppingViewModel.Categories.Select(c => c.CategoryName).ToArray();

                var selectedCategory = await Shell.Current.DisplayActionSheet(
                    $"Wybierz kategorię dla: {product.Name}",
                    "Anuluj",
                    null,
                    categoryNames);

                if (string.IsNullOrWhiteSpace(selectedCategory) || selectedCategory == "Anuluj")
                {
                    await Shell.Current.DisplayAlert("Błąd", "Stwórz kategorię.", "OK");
                    continue;
                }

                var shoppingModel = new Product(product.Name ?? "Produkt")
                {
                    Category = selectedCategory,
                    Value = product.Value,
                    IsChecked = false,
                    Unit = product.SelectedUnit,
                    IsOptional = product.IsOptional
                };

                var newProductVm = new ProductItemViewModel(shoppingModel);
                newProductVm.PropertyChanged += shoppingViewModel.OnItemPropertyChanged;

                var targetGroup = shoppingViewModel.Categories.First(c => c.CategoryName == selectedCategory);
                targetGroup.Add(newProductVm);

                Products.Remove(product);
            }

            if (Products.Count == 0)
            {
                RecipesViewModel.Instance.Recipes.Remove(this);
            }

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }
    }
}