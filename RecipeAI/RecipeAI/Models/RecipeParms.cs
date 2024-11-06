namespace RecipeAI.Models
{
    public class RecipeParms
    {
        public string MealTime { get; set; } = "BreakFast";
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public string? SelectedIdea { get; set; }
    }
}
