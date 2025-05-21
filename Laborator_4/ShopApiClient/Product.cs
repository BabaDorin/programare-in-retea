public partial class ShopApiClient
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; } // Folosim decimal pentru preț
        public int CategoryId { get; set; }
    }
}