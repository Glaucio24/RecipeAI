using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeAI.Data;
using RecipeAI.Models;



namespace RecipeAI.Controller
{
    [Route("[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        [HttpPost, Route("GetRecipeIdeas")]
        public async Task<ActionResult<List<Idea>>> GetRecipeIdeas(RecipeParms recipeParms)
        {
            return SampleData.RecipeIdeas;
        }


    }
}
