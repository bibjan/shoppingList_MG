using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shoppingList.Models
{
    public class Recipe
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        public Recipe(string name, string description = "")
        {
            Name = name;
            Description = description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                Steps.Add(description);
            }
        }
    }
}