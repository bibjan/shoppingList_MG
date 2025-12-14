using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using shoppingList.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System;
using System.IO;
using System.Diagnostics;

namespace shoppingList.ViewModels
{
    public class ShoppingViewModel : ObservableObject
    {
        public static ShoppingViewModel Instance { get; } = new();
        public ObservableCollection<ProductItemViewModel> Products { get; } = new();
        public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

        public IAsyncRelayCommand NewProductCommand { get; set; }
        public IAsyncRelayCommand NewCategoryCommand { get; set; }
        public IAsyncRelayCommand RemoveProductCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public IAsyncRelayCommand ImportCommand { get; }

        public ShoppingViewModel()
        {
            NewProductCommand = new AsyncRelayCommand(AddNewProductAsync);
            NewCategoryCommand = new AsyncRelayCommand(AddNewCategoryAsync);
            RemoveProductCommand = new AsyncRelayCommand<ProductItemViewModel>(RemoveProductAsync);
            ExportCommand = new AsyncRelayCommand(ExportCurrentListAsync);
            ImportCommand = new AsyncRelayCommand(ImportIntoCurrentListAsync);

            var data = ShoppingList.LoadAppData();
            if (data != null)
            {
                Categories.Clear();
                foreach (var c in data.Categories)
                {
                    var catModel = new Category(c.Name);
                    foreach (var p in c.Products)
                    {
                        catModel.Products.Add(new Product(p.Name)
                        {
                            Value = p.Value,
                            Unit = p.Unit,
                            IsOptional = p.IsOptional,
                            Shop = p.Shop,
                            IsChecked = p.IsChecked,
                            Category = c.Name
                        });
                    }
                    var catVm = new CategoryItemViewModel(catModel);

                    foreach (var pvm in catVm)
                    {
                        pvm.PropertyChanged += OnItemPropertyChanged;
                    }

                    Categories.Add(catVm);
                }

                var shopsVm = ShopsViewModel.Instance;
                shopsVm.Shops.Clear();
                foreach (var s in data.Shops)
                {
                    var shopModel = new Shop(s.Name);
                    foreach (var sp in s.Products)
                    {
                        shopModel.Products.Add(new Product(sp.Name)
                        {
                            Value = sp.Quantity,
                            Unit = sp.Unit,
                            IsOptional = sp.IsOptional
                        });
                    }

                    var shopVm = new ShopItemViewModel(shopModel);
                    shopsVm.Shops.Add(shopVm);
                }

                var recipesVm = RecipesViewModel.Instance;
                recipesVm.Recipes.Clear();
                foreach (var r in data.Recipes)
                {
                    var recipeVm = new RecipeItemViewModel(r.Name, r.Steps);
                    foreach (var p in r.Products)
                    {
                        var model = new Product(p.Name)
                        {
                            Value = p.Value,
                            Unit = p.Unit,
                            IsOptional = p.IsOptional,
                            Shop = p.Shop,
                            IsChecked = p.IsChecked,
                            Category = r.Name
                        };
                        recipeVm.Products.Add(new ProductItemViewModel(model));
                    }
                    recipesVm.Recipes.Add(recipeVm);
                }

                if (recipesVm.Recipes.Count == 0)
                {
                    recipesVm.InitializeDefaultRecipes();
                    ShoppingList.SaveFromViewModels(this, ShopsViewModel.Instance, RecipesViewModel.Instance);
                }
            }
            else
            {
                RecipesViewModel.Instance.InitializeDefaultRecipes();
                ShoppingList.SaveFromViewModels(this, ShopsViewModel.Instance, RecipesViewModel.Instance);
            }
        }

        private async Task AddNewProductAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("Nowy produkt",
                "Podaj nazwe produktu:",
                "OK",
                "Anuluj");
            var name = input?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var categoryNames = Categories.Select(c => c.CategoryName).ToArray();

            var selectedCategory = await Shell.Current.DisplayActionSheet(
                "Wybierz kategorię", "Anuluj", null, categoryNames);

            if (string.IsNullOrWhiteSpace(selectedCategory) || selectedCategory == "Anuluj")
            {
                await Shell.Current.DisplayAlert("Błąd", "Wybierz kategorie.", "OK");
                return;
            }

            var newProduct = new ProductItemViewModel(new Product(name)
            {
                Category = selectedCategory
            });

            newProduct.PropertyChanged += OnItemPropertyChanged;

            var targetGroup = Categories.First(c => c.CategoryName == selectedCategory);
            targetGroup.Add(newProduct);

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }

        private async Task AddNewCategoryAsync()
        {
            var input = await Shell.Current.DisplayPromptAsync("Nowa kategoria",
                "Podaj nazwe kategorii:",
                "OK",
                "Anuluj");
            var category = input?.Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                return;
            }

            var catModel = new Category(category);
            var catVm = new CategoryItemViewModel(catModel);

            Categories.Add(catVm);

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }

        private async Task RemoveProductAsync(ProductItemViewModel? product)
        {
            if (product == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Usuń produkt",
                $"Czy na pewno chcesz usunąć produkt '{product.Name}'?",
                "Tak",
                "Nie");

            if (!confirm)
            {
                return;
            }

            var group = Categories.FirstOrDefault(g => g.Contains(product));
            if (group is not null)
            {
                product.PropertyChanged -= OnItemPropertyChanged;
                group.Remove(product);
            }

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }

        public void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductItemViewModel.IsChecked) && sender is ProductItemViewModel vm && vm.IsChecked)
            {
                var group = Categories.FirstOrDefault(g => g.Contains(vm));
                if (group is null) return;

                var from = group.IndexOf(vm);
                var to = group.Count - 1;
                if (from >= 0 && from != to)
                {
                    group.Move(from, to);
                }
            }

            ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);
        }
        public async Task ExportCurrentListAsync()
        {
            var page = App.Current?.MainPage;
            if (page == null) return;

            if (Categories.Count == 0)
            {
                await page.DisplayAlert("Export", "Lista zakupów jest pusta.", "OK");
                return;
            }

            try
            {
                var doc = new XDocument(
                    new XElement("ShoppingList",
                        new XElement("Name", "Lista zakupów"),
                        new XElement("Categories",
                            from c in Categories
                            select new XElement("Category",
                                new XElement("Name", c.CategoryName),
                                new XElement("Products",
                                    from p in c
                                    select new XElement("Product",
                                        new XElement("Name", p.Name ?? string.Empty),
                                        new XElement("Quantity", p.Value),
                                        new XElement("Unit", p.SelectedUnit ?? string.Empty),
                                        new XElement("Optional", p.IsOptional),
                                        new XElement("Store", p.SelectedShop ?? string.Empty),
                                        new XElement("Bought", p.IsChecked)
                                    )
                                )
                            )
                        )
                    )
                );

                string fileName = $"{SanitizeFileName("lista_zakupow")}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

                string saveDir = FileSystem.AppDataDirectory;
                Directory.CreateDirectory(saveDir);
                string savePath = Path.Combine(saveDir, fileName);

                doc.Save(savePath);

                await page.DisplayAlert("Export", $"Plik zapisano: {savePath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                var page2 = App.Current?.MainPage;
                if (page2 != null)
                    await page2.DisplayAlert("Export error", ex.Message, "OK");
            }
        }

        public async Task ImportIntoCurrentListAsync()
        {
            var page = App.Current?.MainPage;
            if (page == null) return;

            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik XML z listą zakupów"
                });

                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                var importedDoc = XDocument.Load(stream);
                var root = importedDoc.Root;
                IEnumerable<XElement> importedElements;

                if (root == null)
                {
                    importedElements = Enumerable.Empty<XElement>();
                }
                else if (root.Name.LocalName == "ShoppingList")
                {
                    importedElements = new[] { root };
                }
                else
                {
                    importedElements = root.Elements("ShoppingList");
                }

                var action = await Shell.Current.DisplayActionSheet(
                    "Co zrobić z importowaną listą?", "Anuluj", null,
                    "Zastąp obecną", "Dodaj do obecnej");

                if (action == "Anuluj" || string.IsNullOrWhiteSpace(action)) return;

                if (action == "Zastąp obecną")
                {
                    foreach (var cat in Categories)
                    {
                        foreach (var p in cat)
                            p.PropertyChanged -= OnItemPropertyChanged;
                    }
                    Categories.Clear();
                }

                int countProducts = 0;

                foreach (var el in importedElements)
                {
                    foreach (var cEl in el.Element("Categories")?.Elements("Category") ?? Enumerable.Empty<XElement>())
                    {
                        var catName = cEl.Element("Name")?.Value ?? string.Empty;

                        var categoryVm = Categories.FirstOrDefault(c => c.CategoryName == catName);

                        Category? catModel = null;
                        if (categoryVm == null)
                        {
                            catModel = new Category(catName);
                            categoryVm = new CategoryItemViewModel(catModel);
                            Categories.Add(categoryVm);
                        }

                        foreach (var pEl in cEl.Element("Products")?.Elements("Product") ?? Enumerable.Empty<XElement>())
                        {
                            var name = pEl.Element("Name")?.Value ?? "";
                            var qtyText = pEl.Element("Quantity")?.Value ?? "0";
                            int qty = int.TryParse(qtyText, out var q) ? q : 0;
                            var unit = pEl.Element("Unit")?.Value ?? "";
                            var optional = bool.TryParse(pEl.Element("Optional")?.Value, out var opt) ? opt : false;
                            var store = pEl.Element("Store")?.Value ?? "";
                            var bought = bool.TryParse(pEl.Element("Bought")?.Value, out var b) ? b : false;

                            var categoryForProduct = catModel?.Name ?? categoryVm.CategoryName;

                            var model = new Product(name)
                            {
                                Value = qty,
                                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                                IsOptional = optional,
                                Shop = string.IsNullOrWhiteSpace(store) ? null : store,
                                IsChecked = bought,
                                Category = categoryForProduct
                            };

                            if (catModel != null)
                            {
                                catModel.Products.Add(model);
                            }

                            var productVm = new ProductItemViewModel(model);
                            productVm.PropertyChanged += OnItemPropertyChanged;
                            categoryVm.Add(productVm);
                            countProducts++;
                        }
                    }
                }

                ShoppingList.SaveFromViewModels(ShoppingViewModel.Instance, ShopsViewModel.Instance, RecipesViewModel.Instance);

                await page.DisplayAlert("Import", $"Zaimportowano {countProducts} produktów.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                var page2 = App.Current?.MainPage;
                if (page2 != null)
                    await page2.DisplayAlert("Import error", ex.Message, "OK");
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "file";
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return cleaned;
        }
    }
}