using shoppingList.ViewModels;
using System.Xml.Serialization;

namespace shoppingList.Models
{
    public static class Data
    {
        private static string FilePath = Path.Combine(FileSystem.AppDataDirectory, "appdata.xml");

        public static void Save()
        {
            try
            {
                var data = new AppData
                {
                    Categories = ShoppingViewModel.Instance.Categories.Select(ToDto).ToList(),
                    Recipes = RecipesViewModel.Instance.Recipes.Select(ToDto).ToList(),
                    Shops = ShopsViewModel.Instance.Shops.Select(s => new ShopData { Name = s.ShopName }).ToList()
                };

                SerializeToFile(data, FilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu: {ex.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    RecipesViewModel.Instance.InitializeDefaultRecipes();
                    return;
                }

                var data = DeserializeFromFile<AppData>(FilePath);
                if (data == null)
                {
                    RecipesViewModel.Instance.InitializeDefaultRecipes();
                    return;
                }

                LoadShops(data.Shops);
                LoadCategories(data.Categories);
                LoadRecipes(data.Recipes);

                if (RecipesViewModel.Instance.Recipes.Count == 0)
                {
                    RecipesViewModel.Instance.InitializeDefaultRecipes();
                }
            }
            catch
            {
                RecipesViewModel.Instance.InitializeDefaultRecipes();
            }
        }

        public static async Task ExportShoppingListAsync()
        {
            try
            {
                if (ShoppingViewModel.Instance.Categories.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Błąd", "Lista zakupów jest pusta.", "OK");
                    return;
                }

                var exportData = new ShoppingListExport
                {
                    ExportDate = DateTime.Now,
                    Categories = ShoppingViewModel.Instance.Categories.Select(ToDto).ToList()
                };

                var fileName = $"lista_zakupow_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                SerializeToFile(exportData, filePath);
                await Shell.Current.DisplayAlert("Sukces", $"Wyeksportowano do:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Błąd", $"Eksport nie powiódł się: {ex.Message}", "OK");
            }
        }

        public static async Task ImportShoppingListAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik z listą zakupów"
                });

                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                var importData = DeserializeFromStream<ShoppingListExport>(stream);

                if (importData?.Categories == null || importData.Categories.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Błąd", "Plik jest pusty lub nieprawidłowy.", "OK");
                    return;
                }

                var action = await Shell.Current.DisplayActionSheet(
                    "Co zrobić z listą?", "Anuluj", null,
                    "Zastąp obecną", "Dodaj do obecnej");

                if (action == "Anuluj" || string.IsNullOrWhiteSpace(action)) return;

                if (action == "Zastąp obecną")
                {
                    ShoppingViewModel.Instance.Categories.Clear();
                }

                LoadCategories(importData.Categories);
                Save();

                var count = importData.Categories.Sum(c => c.Products.Count);
                await Shell.Current.DisplayAlert("Sukces", $"Zaimportowano {count} produktów.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Błąd", $"{ex.Message}", "OK");
            }
        }

        private static void SerializeToFile<T>(T data, string path)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer, data);
        }

        private static T? DeserializeFromFile<T>(string path) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StreamReader(path);
            return serializer.Deserialize(reader) as T;
        }

        private static T? DeserializeFromStream<T>(Stream stream) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            return serializer.Deserialize(stream) as T;
        }

        private static CategoryData ToDto(CategoryItemViewModel c) => new()
        {
            Name = c.CategoryName,
            Products = c.Select(ToDto).ToList()
        };

        private static RecipeData ToDto(RecipeItemViewModel r) => new()
        {
            Name = r.RecipeName,
            Description = r.RecipeDescription,
            Products = r.Products.Select(ToDto).ToList()
        };

        private static ProductData ToDto(ProductItemViewModel p) => new()
        {
            Name = p.Name ?? "",
            Value = p.Value,
            IsChecked = p.IsChecked,
            IsOptional = p.IsOptional,
            Unit = p.SelectedUnit,
            Shop = p.SelectedShop
        };

        private static void LoadShops(List<ShopData> shops)
        {
            ShopsViewModel.Instance.Shops.Clear();
            foreach (var shop in shops)
            {
                ShopsViewModel.Instance.Shops.Add(new ShopItemViewModel(shop.Name));
            }

        }

        private static void LoadCategories(List<CategoryData> categories)
        {
            foreach (var cat in categories)
            {
                var existing = ShoppingViewModel.Instance.Categories
                    .FirstOrDefault(c => c.CategoryName == cat.Name);

                var categoryVm = existing ?? new CategoryItemViewModel(cat.Name);

                if (existing == null)
                    ShoppingViewModel.Instance.Categories.Add(categoryVm);

                foreach (var prod in cat.Products)
                {
                    var productVm = CreateProductVm(prod);
                    productVm.PropertyChanged += ShoppingViewModel.Instance.OnItemPropertyChanged;
                    categoryVm.Add(productVm);
                    AssignToShop(productVm, prod.Shop);
                }
            }
        }

        private static void LoadRecipes(List<RecipeData> recipes)
        {
            RecipesViewModel.Instance.Recipes.Clear();
            foreach (var recipe in recipes)
            {
                var recipeVm = new RecipeItemViewModel(recipe.Name, recipe.Description);
                foreach (var prod in recipe.Products)
                    recipeVm.Products.Add(CreateProductVm(prod));
                RecipesViewModel.Instance.Recipes.Add(recipeVm);
            }
        }

        private static ProductItemViewModel CreateProductVm(ProductData p) =>
            new(new Shopping(p.Name)
            {
                Value = p.Value,
                IsChecked = p.IsChecked,
                IsOptional = p.IsOptional,
                Unit = p.Unit,
                Shop = p.Shop
            });

        private static void AssignToShop(ProductItemViewModel productVm, string? shopName)
        {
            if (string.IsNullOrWhiteSpace(shopName)) return;
            var shop = ShopsViewModel.Instance.Shops.FirstOrDefault(s => s.ShopName == shopName);
            if (shop != null && !shop.Products.Contains(productVm))
            {
                shop.Products.Add(productVm);
            }
                
        }
    }

    [XmlRoot("AppData")]
    public class AppData
    {
        public List<CategoryData> Categories { get; set; } = new();
        public List<RecipeData> Recipes { get; set; } = new();
        public List<ShopData> Shops { get; set; } = new();
    }

    [XmlRoot("ShoppingList")]
    public class ShoppingListExport
    {
        public DateTime ExportDate { get; set; }
        public List<CategoryData> Categories { get; set; } = new();
    }

    public class CategoryData
    {
        public string Name { get; set; } = "";
        public List<ProductData> Products { get; set; } = new();
    }

    public class RecipeData
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ProductData> Products { get; set; } = new();
    }

    public class ShopData
    {
        public string Name { get; set; } = "";
    }

    public class ProductData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public bool IsChecked { get; set; }
        public bool IsOptional { get; set; }
        public string? Unit { get; set; }
        public string? Shop { get; set; }
    }
}