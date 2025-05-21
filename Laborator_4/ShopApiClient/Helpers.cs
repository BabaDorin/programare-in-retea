public partial class ShopApiClient
{
    public static class Helpers
    {
        public static string[] ReadLineAsArgs()
        {
            Console.Write("Enter command: ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            System.Text.RegularExpressions.MatchCollection matches =
                System.Text.RegularExpressions.Regex.Matches(input, "[^\\s\"']+|\"([^\"]*)\"|'([^']*)'");

            List<string> result = new List<string>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                // If a quoted group was captured, use its value; otherwise, use the full match.
                if (match.Groups[1].Success) // Double quotes
                {
                    result.Add(match.Groups[1].Value);
                }
                else if (match.Groups[2].Success) // Single quotes
                {
                    result.Add(match.Groups[2].Value);
                }
                else
                {
                    result.Add(match.Value);
                }
            }
            return result.ToArray();
        }
    }
}
