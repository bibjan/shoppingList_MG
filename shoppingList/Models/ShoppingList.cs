using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using shoppingList.ViewModels;

namespace shoppingList.Models
{
    public static class ShoppingList
    {
        private static string AppDataPath => Path.Combine(FileSystem.AppDataDirectory, "appdata.xml");

        public static void SaveFromViewModels(ShoppingViewModel shoppingVm, ShopsViewModel shopsVm, RecipesViewModel recipesVm)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppDataPath) ?? string.Empty);

                var categoriesEl = new XElement("Categories",
                    from c in shoppingVm.Categories
                    select new XElement("Category",
                        new XElement("Name", c.CategoryName),
                        new XElement("Products",
                            from p in c
                            select new XElement("Product",
                                new XElement("Name", p.Name ?? string.Empty),
                                new XElement("Quantity", p.Value),
                                new XElement("Unit", p.SelectedUnit ?? string.Empty),
                                new XElement("Optional", p.IsOptional),
                                new XElement("Shop", p.SelectedShop ?? string.Empty),
                                new XElement("Checked", p.IsChecked)
                            )
                        )
                    )
                );

                var recipesEl = new XElement("Recipes",
                    from r in recipesVm.Recipes
                    select new XElement("Recipe",
                        new XElement("Name", r.RecipeName),
                        new XElement("Steps",
                            from s in r.Steps
                            select new XElement("Step", s)
                        ),
                        new XElement("Products",
                            from p in r.Products
                            select new XElement("Product",
                                new XElement("Name", p.Name ?? string.Empty),
                                new XElement("Quantity", p.Value),
                                new XElement("Unit", p.SelectedUnit ?? string.Empty),
                                new XElement("Optional", p.IsOptional),
                                new XElement("Shop", p.SelectedShop ?? string.Empty),
                                new XElement("Checked", p.IsChecked)
                            )
                        )
                    )
                );

                var shopsEl = new XElement("Shops",
                    from s in shopsVm.Shops
                    select new XElement("Shop",
                        new XElement("Name", s.ShopName),
                        new XElement("Products",
                            from prod in s.Products
                            select new XElement("Product",
                                new XElement("Name", prod.Name ?? string.Empty),
                                new XElement("Quantity", prod.Value),
                                new XElement("Unit", prod.SelectedUnit ?? string.Empty),
                                new XElement("Optional", prod.IsOptional)
                            )
                        )
                    )
                );

                var doc = new XDocument(
                    new XElement("AppData",
                        categoriesEl,
                        recipesEl,
                        shopsEl
                    )
                );

                doc.Save(AppDataPath);
                Trace.WriteLine($"Saved appdata to: {AppDataPath}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Storage.SaveFromViewModels error: {ex.Message}");
            }
        }

        public static AppData? LoadAppData()
        {
            try
            {
                if (!File.Exists(AppDataPath))
                    return null;

                var doc = XDocument.Load(AppDataPath);
                var root = doc.Root;
                if (root == null)
                    return null;

                var data = new AppData();

                foreach (var cEl in root.Element("Categories")?.Elements("Category") ?? Enumerable.Empty<XElement>())
                {
                    var cat = new CategoryData { Name = cEl.Element("Name")?.Value ?? "" };
                    foreach (var pEl in cEl.Element("Products")?.Elements("Product") ?? Enumerable.Empty<XElement>())
                    {
                        cat.Products.Add(new ProductData
                        {
                            Name = pEl.Element("Name")?.Value ?? "",
                            Value = int.TryParse(pEl.Element("Quantity")?.Value, out var v) ? v : 0,
                            Unit = pEl.Element("Unit")?.Value,
                            IsOptional = bool.TryParse(pEl.Element("Optional")?.Value, out var opt) ? opt : false,
                            Shop = pEl.Element("Shop")?.Value,
                            IsChecked = bool.TryParse(pEl.Element("Checked")?.Value, out var ch) ? ch : false
                        });
                    }
                    data.Categories.Add(cat);
                }

                foreach (var rEl in root.Element("Recipes")?.Elements("Recipe") ?? Enumerable.Empty<XElement>())
                {
                    var rd = new RecipeData
                    {
                        Name = rEl.Element("Name")?.Value ?? "",
                        Steps = rEl.Element("Steps")?.Elements("Step").Select(x => x.Value ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>()
                    };

                    foreach (var pEl in rEl.Element("Products")?.Elements("Product") ?? Enumerable.Empty<XElement>())
                    {
                        rd.Products.Add(new ProductData
                        {
                            Name = pEl.Element("Name")?.Value ?? "",
                            Value = int.TryParse(pEl.Element("Quantity")?.Value ?? pEl.Element("Value")?.Value ?? "0", out var v) ? v : 0,
                            Unit = pEl.Element("Unit")?.Value,
                            IsOptional = bool.TryParse(pEl.Element("Optional")?.Value, out var opt) ? opt : false,
                            Shop = pEl.Element("Shop")?.Value,
                            IsChecked = bool.TryParse(pEl.Element("Checked")?.Value, out var ch) ? ch : false
                        });
                    }

                    data.Recipes.Add(rd);
                }

                foreach (var sEl in root.Element("Shops")?.Elements("Shop") ?? Enumerable.Empty<XElement>())
                {
                    var sd = new ShopData { Name = sEl.Element("Name")?.Value ?? "" };
                    foreach (var pEl in sEl.Element("Products")?.Elements("Product") ?? Enumerable.Empty<XElement>())
                    {
                        sd.Products.Add(new ShopProductData
                        {
                            Name = pEl.Element("Name")?.Value ?? "",
                            Quantity = int.TryParse(pEl.Element("Quantity")?.Value, out var q) ? q : 0,
                            Unit = pEl.Element("Unit")?.Value,
                            IsOptional = bool.TryParse(pEl.Element("Optional")?.Value, out var opt) ? opt : false
                        });
                    }
                    data.Shops.Add(sd);
                }

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Storage.LoadAppData error: {ex.Message}");
                return null;
            }
        }

        public class AppData
        {
            public List<CategoryData> Categories { get; set; } = new();
            public List<RecipeData> Recipes { get; set; } = new();
            public List<ShopData> Shops { get; set; } = new();
        }

        public class CategoryData
        {
            public string Name { get; set; } = "";
            public List<ProductData> Products { get; set; } = new();
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

        public class RecipeData
        {
            public string Name { get; set; } = "";
            public List<string> Steps { get; set; } = new();
            public List<ProductData> Products { get; set; } = new();
        }

        public class ShopData
        {
            public string Name { get; set; } = "";
            public List<ShopProductData> Products { get; set; } = new();
        }

        public class ShopProductData
        {
            public string Name { get; set; } = "";
            public int Quantity { get; set; }
            public string? Unit { get; set; }
            public bool IsOptional { get; set; }
        }
    }
}