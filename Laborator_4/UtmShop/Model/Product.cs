using System;

namespace UtmShop.Model
{
    public class Product
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public Decimal Price { get; set; }
        public Category ParentCategory { get; set; }
    }
}
