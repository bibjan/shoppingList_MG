using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shoppingList.Models
{
    public class Shopping
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Value { get; set; }

        public bool IsChecked { get; set; }

        public bool IsOptional { get; set; }
        public string? Unit { get; set; }
        public string? Shop { get; set; }

        public Shopping(string name)
        {
            Name = name;
        }

        public void Add()
        {
            Value++;
        }

        public void Subtract()
        {
            if (Value > 0)
            {
                Value--;
            }
        }
    }
}
