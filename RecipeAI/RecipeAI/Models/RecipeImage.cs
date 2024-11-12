namespace RecipeAI.Models
{
    public class RecipeImage
    {
        public int Created { get; set; }  // Assuming 'created' is a number (e.g., timestamp)
        public ImageData[]? Data { get; set; }
    }

    public class ImageData
    {
        public string? Url { get; set; }
    }

}
