using shoppingList.Models;

namespace shoppingList
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Data.Load();

            MainPage = new AppShell();
        }
    }
}
