using RecipeAI.Models;

namespace RecipeAI.Services
{
    public interface IOpenAIAPI
    {
        Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingredients);
    }
}
