using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using shoppingList.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

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
            ExportCommand = new AsyncRelayCommand(Data.ExportShoppingListAsync);
            ImportCommand = new AsyncRelayCommand(Data.ImportShoppingListAsync);
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

            var newProduct = new ProductItemViewModel(new Shopping(name)
            {
                Category = selectedCategory
            });

            newProduct.PropertyChanged += OnItemPropertyChanged;

            var targetGroup = Categories.First(c => c.CategoryName == selectedCategory);
            targetGroup.Add(newProduct);

            Data.Save();
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

            Categories.Add(new CategoryItemViewModel(category));
            Data.Save();
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

            Data.Save();
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

            Data.Save();
        }
    }
}