using RecipeAI.ChatEndPoint;
using RecipeAI.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Linq.Expressions;
using System;
using System.Reflection.Metadata.Ecma335;
using RecipeAI.Components.Pages;

namespace RecipeAI.Services
{
    public class OpenAIService : IOpenAIAPI
    {
        private readonly IConfiguration _configuration;
        private static readonly string _baseUrl = "https://api.openai.com/v1/chat/completions";
        private static readonly HttpClient _httpClient = new();
        private readonly JsonSerializerOptions _jsonOptions;

        // build the function object so the AI will return JSON formatted object
        private static ChatFunction.Parameter _recipeIdeaParameter = new()
        {
            // describes one Idea
            Type = "object",
            Required = new string[] { "Index", "Title", "Description" },
            Properties = new
            {
                // provide a type and description for each property of the Idea model
                Index = new ChatFunction.Property
                {
                    Type = "number",
                    Description = "A unique identifier for this object",
                },
                Title = new ChatFunction.Property
                {
                    Type = "string",
                    Description = "The name for a recipe to create"
                },
                Description = new ChatFunction.Property
                {
                    Type = "string",
                    Description = "A description of the recipe"
                }
            }
        };

        private static ChatFunction _ideaFunction = new()
        {
            // describe the function we want an argument for from the AI
            Name = "CreateRecipe",
            // this description ensures we get 5 ideas back
            Description = "Generates recipes for each idea in an array of five recipe ideas",
            Parameters = new
            {
                // OpenAI requires that the parameters are an object, so we need to wrap our array in an object
                Type = "object",
                Properties = new
                {
                    Data = new // our array will come back in an object in the Data property
                    {
                        Type = "array",
                        // further ensures the AI will create 5 recipe ideas
                        Description = "An array of five recipe ideas",
                        Items = _recipeIdeaParameter
                    }
                }
            }
        };

        private static ChatFunction.Parameter _recipeParameter = new()
        {
            Type = "object",
            Description = "The recipe to display",
            Required = new[] { "Title", "Ingredients", "Instructions", "Summary" },
            Properties = new
            {
                Title = new
                {
                    Type = "string",
                    Description = "The title of the recipe to display",
                },
                Ingredients = new
                {
                    Type = "array",
                    Description = "An array of all the ingredients mentioned in the recipe instructions",
                    Items = new { Type = "string" }
                },
                Instructions = new
                {
                    Type = "array",
                    Description = "An array of each step for cooking this recipe",
                    Items = new { Type = "string" }
                },
                Summary = new
                {
                    Type = "string",
                    Description = "A summary description of what this recipe creates",
                },
            },
        };

        private static ChatFunction _recipeFunction = new()
        {
            Name = "DisplayRecipe",
            Description = "Displays the recipe from the parameter to the user",
            Parameters = new
            {
                Type = "object",
                Properties = new
                {
                    Data = _recipeParameter
                },
            }
        };

        public OpenAIService(IConfiguration configuration)
        {
            _configuration = configuration;
            var apiKey = _configuration["OpenAI:OpenAiKey"] ?? Environment.GetEnvironmentVariable("OpenAiKey");


            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);

            _jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingredientList)
        {
            string url = $"{_baseUrl}";
            string systemPrompt = "You are a world-renewed chef. I will send you a list of ingredients and meal time. You will respond with 5 ideas for dishes.";
            string userPrompt = "";
            string ingredientPrompt = "";

            string ingredients = string.Join(",", ingredientList);

            if (string.IsNullOrEmpty(ingredients))
            {
                ingredientPrompt = "Suggest some ingredients for me.";
            }
            else
            {
                ingredientPrompt = $"I have {ingredients}";
            }
            userPrompt = $"The meal I want to cook is {mealtime}. {ingredientPrompt}";


            ChatMessage systemMessage = new()
            {
                Role = "system",
                Content = $"{systemPrompt}"
            };

            ChatMessage userMessage = new()
            {
                Role = "user",
                Content = $"{userPrompt}"
            };

            ChatRequest request = new()
            {
                Model = "gpt-3.5-turbo",
                Messages = new[] { systemMessage, userMessage },
                Functions = new[] { _ideaFunction },
                FunctionCall = new { Name = _ideaFunction.Name }
            };

            // Send the request to OpenAI API
            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);

            ChatResponse? response = await httpResponse.Content.ReadFromJsonAsync<ChatResponse>();

            // Get the function response containing the recipe ideas
            ChatFunctionResponse? functionResponse = response?.Choices
                                                                      .FirstOrDefault(m => m.Message?.FunctionCall is not null)?
                                                                        .Message?
                                                                            .FunctionCall;

            Result<List<Idea>>? ideasResult = new();

            // Deserialize the arguments to extract the recipe ideas
            if (functionResponse?.Arguments is not null)
            {
                try
                {
                    // Return the recipe ideas directly
                    ideasResult = JsonSerializer.Deserialize<Result<List<Idea>>>(functionResponse.Arguments, _jsonOptions);
                }
                catch (Exception ex)
                {
                    ideasResult = new()
                    {
                        Exception = ex,
                        ErrorMessage = await httpResponse.Content.ReadAsStringAsync()
                    };
                }
            }
            return ideasResult?.Data ?? new List<Idea>();
        }

        public async Task<Recipe?> CreateRecipe(string title, List<string> ingredients)
        {
            string Url = $"{_baseUrl}";
            string systemPrompt = "You are a world-renowned chef. Create the recipe with ingredients, instructions and a summary";
            string userPrompt = $"Create a {title} recipe.";

            ChatMessage userMessage = new()
            {
                Role = "user",
                Content = $"{systemPrompt} {userPrompt}"
            };
            ChatRequest request = new()
            {
                Model = "gpt-3.5-turbo",
                Messages = new[] { userMessage },
                Functions = new[] { _recipeFunction },
                FunctionCall = new { Name = _recipeFunction.Name }
            };
            HttpResponseMessage httpresponse = await _httpClient.PostAsJsonAsync(Url, request, _jsonOptions);
            ChatResponse? response = await httpresponse.Content.ReadFromJsonAsync<ChatResponse?>();

            ChatFunctionResponse? functionResponse = response.Choices
                                                            .FirstOrDefault(m => m.Message?.FunctionCall is not null)
                                                            .Message?
                                                            .FunctionCall;
            Result<Recipe>? recipe = new();
            if (functionResponse?.Arguments is not null)
            {
                try
                {
                    recipe = JsonSerializer.Deserialize<Result<Recipe>>(functionResponse.Arguments, _jsonOptions);
                }
                catch (Exception ex)
                {
                    recipe = new()
                    {
                        Exception = ex,
                        ErrorMessage = await httpresponse.Content.ReadAsStringAsync()
                    };

                }
            }

            return recipe?.Data;
        }

        public async Task<RecipeImage?> CreateRecipeImage(string recipeTitle)
        {
            string url = "https://api.openai.com/v1/images/generations";
            string userPrompt = $"Create a high-quality restaurant product shot for {recipeTitle}";

            // Construct the request object for OpenAI's DALL-E endpoint
            var request = new
            {
                prompt = userPrompt,
                size = "512x512", // Specify the image size
                n = 1 // Request a single image
            };

            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);

            RecipeImage? recipeImage = null;
            try
            {
                // Deserialize the response into your RecipeImage model
                recipeImage = await httpResponse.Content.ReadFromJsonAsync<RecipeImage>(_jsonOptions);

                // Check if Data array has an image URL
                if (recipeImage?.Data?.Length > 0)
                {
                    Console.WriteLine("Image URL: " + recipeImage.Data[0].Url);
                }
                else
                {
                    Console.WriteLine("Error: No image data returned.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Recipe image could not be retrieved. Details: " + ex.Message);
            }
            return recipeImage;
        }


    }
}