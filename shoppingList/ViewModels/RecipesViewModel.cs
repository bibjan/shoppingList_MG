using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using shoppingList.Models;

namespace shoppingList.ViewModels
{
    public class RecipesViewModel : ObservableObject
    {
        public IAsyncRelayCommand NewRecipeCommand { get; set; }
        public IAsyncRelayCommand AddProductCommand { get; set; }

        public static RecipesViewModel Instance { get; } = new();

        public ObservableCollection<RecipeItemViewModel> Recipes { get; } = new();

        public RecipesViewModel()
        {
            NewRecipeCommand = new AsyncRelayCommand(NewRecipeAsync);
            AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        }

        public void InitializeDefaultRecipes()
        {
            var scrambledEggs = new RecipeItemViewModel("Jajecznica", "Na patelni rozpuść masło. Dodaj jajka i na średnim ogniu mieszaj jajka do ścięcia. W trakcie mieszania dodaj szynke i sól.");
            scrambledEggs.Products.Add(new ProductItemViewModel(new Shopping("Jajka") { Value = 3, Unit = "szt." }));
            scrambledEggs.Products.Add(new ProductItemViewModel(new Shopping("Masło") { Value = 15, Unit = "g" }));
            scrambledEggs.Products.Add(new ProductItemViewModel(new Shopping("Szynka") { Value = 100, Unit = "g" }));
            scrambledEggs.Products.Add(new ProductItemViewModel(new Shopping("Sól") { Value = 1, Unit = "szczypta" }));
            Recipes.Add(scrambledEggs);

            var pancakes = new RecipeItemViewModel("Naleśniki", "Wymieszaj mąkę z jajkami, stopniowo dodając mleko. Dodaj szczyptę cukru i roztopione masło. Smaż cienkie placki na rozgrzanej patelni po ok. 1-2 min z każdej strony.");
            pancakes.Products.Add(new ProductItemViewModel(new Shopping("Mąka") { Value = 250, Unit = "g" }));
            pancakes.Products.Add(new ProductItemViewModel(new Shopping("Mleko") { Value = 500, Unit = "ml" }));
            pancakes.Products.Add(new ProductItemViewModel(new Shopping("Jajka") { Value = 2, Unit = "szt." }));
            pancakes.Products.Add(new ProductItemViewModel(new Shopping("Cukier") { Value = 2, Unit = "szczypta" }));
            pancakes.Products.Add(new ProductItemViewModel(new Shopping("Masło") { Value = 5, Unit = "g" }));
            Recipes.Add(pancakes);

            Data.Save();
        }

        private async Task NewRecipeAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("Nowy przepis",
                "Podaj nazwę przepisu:",
                "OK",
                "Anuluj");
            var recipeName = input?.Trim();

            if (string.IsNullOrWhiteSpace(recipeName))
            {
                return;
            }

            var input2 = await Shell.Current.DisplayPromptAsync("Dodaj instrukcje",
                "Podaj instrukcje przepisu:",
                "OK",
                "Anuluj");
            var recipeDesc = input2?.Trim();

            if (string.IsNullOrWhiteSpace(recipeDesc))
            {
                return;
            }

            Recipes.Add(new RecipeItemViewModel(recipeName, recipeDesc));

            Data.Save();
        }

        private async Task AddProductAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("Nowy produkt",
                "Podaj nazwę produktu:",
                "OK",
                "Anuluj");
            var name = input?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var recipeNames = Recipes.Select(r => r.RecipeName).ToArray();

            var selectedRecipe = await Shell.Current.DisplayActionSheet(
                "Wybierz przepis", "Anuluj", null, recipeNames);

            if (string.IsNullOrWhiteSpace(selectedRecipe) || selectedRecipe == "Anuluj")
            {
                await Shell.Current.DisplayAlert("Błąd", "Wybierz przepis.", "OK");
                return;
            }

            var units = new[] { "szt.", "g", "kg", "ml", "l", "łyżki", "szczypta" };
            var selectedUnit = await Shell.Current.DisplayActionSheet(
                "Wybierz jednostkę", "Anuluj", null, units);

            if (string.IsNullOrWhiteSpace(selectedUnit) || selectedUnit == "Anuluj")
            {
                selectedUnit = "szt.";
            }

            var newProduct = new ProductItemViewModel(new Shopping(name)
            {
                Category = selectedRecipe,
                Unit = selectedUnit
            });

            var targetGroup = Recipes.First(r => r.RecipeName == selectedRecipe);
            targetGroup.Products.Add(newProduct);

            Data.Save();
        }
    }
}