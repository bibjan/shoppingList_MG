using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using shoppingList.Models;

namespace shoppingList.ViewModels
{
    public class RecipeItemViewModel : ObservableObject
    {
        public string RecipeName { get; }
        public string RecipeDescription { get; }

        public IAsyncRelayCommand ImportToListCommand { get; }
        public ObservableCollection<ProductItemViewModel> Products { get; } = new();

        public RecipeItemViewModel(string recipeName, string recipeDescription)
        {
            RecipeName = recipeName;
            RecipeDescription = recipeDescription;
            ImportToListCommand = new AsyncRelayCommand(ImportToListAsync);
        }

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

                var shoppingModel = new Shopping(product.Name ?? "Produkt")
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

            Data.Save();
        }
    }
}