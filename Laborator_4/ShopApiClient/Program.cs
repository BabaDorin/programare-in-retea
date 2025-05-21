using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public partial class ShopApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ShopApiClient(string baseUrl = "localhost", int port = 5001)
    {
        _baseUrl = $"https://{baseUrl}:{port}"; 

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                Console.WriteLine($"Warning: SSL certificate validation for {message.RequestUri} is bypassed.");
                return true; // Permite toate certificatele
            }
        };
        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object content = null)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
        if (content != null)
        {
            string jsonContent = JsonSerializer.Serialize(content);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode) 
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    return default(T);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent || (response.StatusCode == System.Net.HttpStatusCode.OK && string.IsNullOrWhiteSpace(responseBody)))
                {
                    return default(T);
                }
                return JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                throw new HttpRequestException($"Request to {method} {endpoint} failed. Status Code: {response.StatusCode}.\nResponse Body: {responseBody}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error with {method} request to {endpoint}: {e.Message}", e);
        }
    }

    // Metodele API
    public async Task GetCategoriesAsync()
    {
        var categories = await SendRequestAsync<List<CategoryShortDto>>(HttpMethod.Get, "/api/Category/categories");
        Console.WriteLine("Categories:");
        if (categories != null)
        {
            foreach (var cat in categories)
            {
                Console.WriteLine($"- ID: {cat.Id}, Name: {cat.Name}");
            }
        }
        else
        {
            Console.WriteLine("No categories found or empty response.");
        }
    }

    public async Task GetCategoryAsync(int categoryId)
    {
        var category = (await SendRequestAsync<List<CategoryShortDto>>(HttpMethod.Get, $"/api/Category/categories/{categoryId}")).FirstOrDefault();
        Console.WriteLine($"Category {categoryId}:");
        if (category != null)
        {
            Console.WriteLine($"- ID: {category.Id}, Name: {category.Name}");
        }
        else
        {
            Console.WriteLine("Category not found or empty response.");
        }
    }

    public async Task CreateCategoryAsync(string title)
    {
        var categoryData = new CreateCategoryDto { Title = title };
        var createdCategory = await SendRequestAsync<CategoryShortDto>(HttpMethod.Post, "/api/Category/categories", categoryData);
        Console.WriteLine("Created Category:");
        if (createdCategory != null)
        {
            Console.WriteLine($"- ID: {createdCategory.Id}, Title: {createdCategory.Name}");
        }
        else
        {
            Console.WriteLine("Category creation failed or no content returned.");
        }
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        await SendRequestAsync<object>(HttpMethod.Delete, $"/api/Category/categories/{categoryId}");
        Console.WriteLine($"Category {categoryId} deleted successfully (or request processed without error).");
    }

    public async Task UpdateCategoryAsync(int categoryId, string newTitle)
    {
        var categoryData = new CreateCategoryDto { Title = newTitle };
        var updatedCategory = await SendRequestAsync<CategoryShortDto>(HttpMethod.Put, $"/api/Category/{categoryId}", categoryData);
        Console.WriteLine("Updated Category:");
        if (updatedCategory != null)
        {
            Console.WriteLine($"- ID: {updatedCategory.Id}, Title: {updatedCategory.Name}");
        }
        else
        {
            Console.WriteLine("Category update failed or no content returned.");
        }
    }

    public async Task CreateProductAsync(int categoryId, Product productData) // Product DTO
    {
        var createdProductResponse = await SendRequestAsync<Product>(HttpMethod.Post, $"/api/Category/categories/{categoryId}/products", productData);
        Console.WriteLine("New product created (response):");
        if (createdProductResponse != null)
        {
            Console.WriteLine($"- ID: {createdProductResponse.Id}, Title: {createdProductResponse.Title}, Price: {createdProductResponse.Price}, CategoryID: {createdProductResponse.CategoryId}");
        }
        else
        {
            Console.WriteLine("Product creation failed or no content returned.");
        }
    }

    public async Task GetProductsAsync(int categoryId)
    {
        var products = await SendRequestAsync<List<Product>>(HttpMethod.Get, $"/api/Category/categories/{categoryId}/products");
        Console.WriteLine($"Products for category {categoryId}:");
        if (products != null && products.Any())
        {
            foreach (var prod in products)
            {
                Console.WriteLine($"- ID: {prod.Id}, Title: {prod.Title}, Price: {prod.Price}");
            }
        }
        else
        {
            Console.WriteLine("No products found in this category or empty response.");
        }
    }


    public static async Task Main(string[] args)
    {
        string methodStr = null;
        int? categoryId = null;
        string jsonData = null; 
        string baseAddr = "localhost";
        int port = 5001;
        bool getProductsFlag = false; 

        PrintUsage();

        while (true)
        {

            Console.Write("> ");
            var commands = Helpers.ReadLineAsArgs();

            for (int i = 0; i < commands.Length; i++)
            {
                switch (commands[i].ToLower())
                {
                    case "-m":
                    case "--method":
                        if (i + 1 < commands.Length) methodStr = commands[++i].ToUpper();
                        else { Console.WriteLine("Method not specified after -m."); }
                        break;
                    case "-i":
                    case "--id":
                        if (i + 1 < commands.Length && int.TryParse(commands[++i], out int idVal)) categoryId = idVal;
                        else { Console.WriteLine("Category ID not specified or invalid after -i."); }
                        break;
                    case "-d":
                    case "--data":
                        if (i + 1 < commands.Length) jsonData = commands[++i];
                        else { Console.WriteLine("Data not specified after -d."); }
                        break;
                }
            }
            if (string.IsNullOrEmpty(methodStr))
            {
                Console.WriteLine("HTTP method (-m) is required.");
                PrintUsage();
            }

            ShopApiClient apiClient = new ShopApiClient(baseAddr, port);

            try
            {
                switch (methodStr)
                {
                    case "GET":
                        if (categoryId.HasValue)
                        {
                            if (!string.IsNullOrEmpty(jsonData) && jsonData.Equals("products", StringComparison.OrdinalIgnoreCase))
                            {
                                await apiClient.GetProductsAsync(categoryId.Value);
                            }
                            else
                            {
                                await apiClient.GetCategoryAsync(categoryId.Value);
                            }
                        }
                        else
                        {
                            await apiClient.GetCategoriesAsync();
                        }
                        break;

                    case "POST":
                        if (string.IsNullOrEmpty(jsonData))
                        {
                            Console.WriteLine("Data (-d) is required for POST request."); break;
                        }
                        if (categoryId.HasValue) // Creare produs într-o categorie
                        {
                            try
                            {
                                var productData = JsonSerializer.Deserialize<CreateProductDto>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (productData == null)
                                {
                                    Console.WriteLine("Invalid product JSON data for POST."); break;
                                }
                                await apiClient.CreateProductAsync(categoryId.Value, new Product
                                {
                                    Id = productData.Id, // Poate fi ignorat de server
                                    Title = productData.Title,
                                    Price = productData.Price,
                                    CategoryId = productData.CategoryId != 0 ? productData.CategoryId : categoryId.Value // Prioritizează ID-ul din JSON dacă e setat
                                });
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"Invalid JSON format for product data: {ex.Message}");
                            }
                        }
                        else
                        {
                            await apiClient.CreateCategoryAsync(jsonData);
                        }
                        break;

                    case "DELETE":
                        if (!categoryId.HasValue)
                        {
                            Console.WriteLine("Category ID (-i) is required for DELETE request."); break;
                        }
                        await apiClient.DeleteCategoryAsync(categoryId.Value);
                        break;

                    case "PUT":
                        if (!categoryId.HasValue || string.IsNullOrEmpty(jsonData))
                        {
                            Console.WriteLine("Category ID (-i) and Data (-d) are required for PUT request."); return;
                        }
                        await apiClient.UpdateCategoryAsync(categoryId.Value, jsonData);
                        break;

                    default:
                        Console.WriteLine($"Unsupported HTTP method: {methodStr}");
                        PrintUsage();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                if (e.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
                }
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("\nUsage: ShopApiClientApp.exe -m <METHOD> [-i <category_id>] [-d <data_json_or_title>]");
        Console.WriteLine("Methods: GET, POST, PUT, DELETE");
        Console.WriteLine("Examples:");
        Console.WriteLine("  -m GET");
        Console.WriteLine("  -m GET -i 1");
        Console.WriteLine("  -m GET -i 1 -d products");
        Console.WriteLine("  -m POST -d \"New Category Title\"");
        Console.WriteLine("  -m POST -i 1 -d '{\"title\":\"New Product\",\"price\":9.99}'"); 
        Console.WriteLine("  -m PUT -i 1 -d \"Updated Category Title\"");
        Console.WriteLine("  -m DELETE -i 1");
    }
}