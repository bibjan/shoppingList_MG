using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using shoppingList.Models;

namespace shoppingList.ViewModels
{
    public class CategoryItemViewModel : ObservableCollection<ProductItemViewModel>
    {
        public string CategoryName { get; }

        private bool _isExpanded = true;

        public char ExpandButtonSymbol => IsExpanded ? 'v' : '>';

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsExpanded)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(ExpandButtonSymbol)));
                }
            }
        }

        public IRelayCommand ToggleExpandCommand { get; }

        public CategoryItemViewModel(Category category)
        {
            CategoryName = category?.Name ?? string.Empty;
            ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);

            if (category?.Products != null)
            {
                foreach (var p in category.Products)
                {
                    var pvm = new ProductItemViewModel(p);
                    this.Add(pvm);
                }
            }
        }
    }
}