using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using shoppingList.Models;
using System.Collections.Generic;
using System;

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
            var scrambledEggs = new Recipe("Jajecznica");
            scrambledEggs.Steps.AddRange(new List<string>
            {
                "Na patelni rozpuść masło.",
                "Wbij jajka i mieszaj na średnim ogniu, aż się zetną.",
                "Dodaj pokrojoną szynkę i sól do smaku, wymieszaj."
            });
            scrambledEggs.Products.Add((new Product("Jajka") { Value = 3, Unit = "szt." }));
            scrambledEggs.Products.Add((new Product("Masło") { Value = 15, Unit = "g" }));
            scrambledEggs.Products.Add((new Product("Szynka") { Value = 100, Unit = "g" }));
            scrambledEggs.Products.Add((new Product("Sól") { Value = 1, Unit = "szczypta" }));

            var scrambledVm = new RecipeItemViewModel(scrambledEggs.Name, scrambledEggs.Steps);
            foreach (var p in scrambledEggs.Products)
            {
                scrambledVm.Products.Add(new ProductItemViewModel(p));
            }
            Recipes.Add(scrambledVm);

            var pancakes = new Recipe("Naleśniki");
            pancakes.Steps.AddRange(new List<string>
            {
                "Wymieszaj mąkę z jajkami i stopniowo dolewaj mleko, aż powstanie gładkie ciasto.",
                "Dodaj szczyptę soli i trochę roztopionego masła do ciasta.",
                "Smaż cienkie placki na rozgrzanej patelni po 1-2 min z każdej strony."
            });
            pancakes.Products.Add((new Product("Mąka") { Value = 250, Unit = "g" }));
            pancakes.Products.Add((new Product("Mleko") { Value = 500, Unit = "ml" }));
            pancakes.Products.Add((new Product("Jajka") { Value = 2, Unit = "szt." }));
            pancakes.Products.Add((new Product("Cukier") { Value = 2, Unit = "szczypta" }));
            pancakes.Products.Add((new Product("Masło") { Value = 5, Unit = "g" }));

            var pancakesVm = new RecipeItemViewModel(pancakes.Name, pancakes.Steps);
            foreach (var p in pancakes.Products)
            {
                pancakesVm.Products.Add(new ProductItemViewModel(p));
            }
            Recipes.Add(pancakesVm);
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

            var useNumberedSteps = await Shell.Current.DisplayAlert(
                "Instrukcje",
                "Czy chcesz dodać instrukcje?",
                "Tak",
                "Nie");

            var steps = new List<string>();

            if (useNumberedSteps)
            {
                int index = 1;

                while (true)
                {
                    var stepInput = await Shell.Current.DisplayPromptAsync(
                        $"Krok {index}",
                        $"Podaj treść kroku {index}:",
                        "Dodaj",
                        "Zakończ");

                    var step = stepInput?.Trim();
                    if (string.IsNullOrWhiteSpace(step))
                    {
                        if (steps.Count == 0)
                        {
                            var fallback = await Shell.Current.DisplayPromptAsync("Dodaj instrukcje",
                                "Podaj instrukcje przepisu:",
                                "OK",
                                "Anuluj");
                            var fallbackText = fallback?.Trim() ?? "";
                            if (string.IsNullOrWhiteSpace(fallbackText))
                                return;
                            steps.Add(fallbackText);
                            break;
                        }
                        break;
                    }

                    steps.Add(step);
                    index++;

                    var addMore = await Shell.Current.DisplayAlert(
                        "Dodaj kolejny krok?",
                        "Czy chcesz dodać kolejny krok?",
                        "Tak",
                        "Nie");

                    if (!addMore) break;
                }

                if (steps.Count == 0)
                {
                    return;
                }
            }
            else
            {
                var input2 = await Shell.Current.DisplayPromptAsync("Dodaj instrukcje",
                    "Podaj instrukcje przepisu:",
                    "OK",
                    "Anuluj");
                var recipeDesc = input2?.Trim();

                if (string.IsNullOrWhiteSpace(recipeDesc))
                {
                    return;
                }

                steps.Add(recipeDesc);
            }

            Recipes.Add(new RecipeItemViewModel(recipeName, steps));

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
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

            var newProduct = new ProductItemViewModel(new Product(name)
            {
                Category = selectedRecipe,
                Unit = selectedUnit
            });

            var targetGroup = Recipes.First(r => r.RecipeName == selectedRecipe);
            targetGroup.Products.Add(newProduct);

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }
    }
}