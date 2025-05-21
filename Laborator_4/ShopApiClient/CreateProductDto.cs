public partial class ShopApiClient
{
    public class CreateProductDto
    {
        public int Id { get; set; } = 0; // De obicei, ID-ul e generat de server la creare
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; } = 0; // Va fi setat dinamic sau ignorat dacă e în URL
    }
}