using RecipeAI.ChatEndPoint;
using RecipeAI.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Linq.Expressions;
using System;
using System.Reflection.Metadata.Ecma335;

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

        public OpenAIService(IConfiguration configuration)
        {
            _configuration = configuration;
            var apiKey = _configuration["OpenAIService:OpenAiKey"] ?? Environment.GetEnvironmentVariable("OpenAiKey");

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
            string url = $"{_baseUrl}chat/completions";
            string systemPrompt = "You are a world-renewed chef. I will send you a list of ingredients and meal time. You will respond with 5 ideas for dishes. ";
            string userPrompt = "";
            string ingredientPrompt = "";

            string ingredients = string.Join(",", ingredientList);

            if (string.IsNullOrEmpty(ingredients))
            {
                ingredientPrompt = "Suggest some ingredients for me";
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
                Content = $"{systemPrompt}"
            };
            ChatRequest request = new()
            {
                Model = "gpt-3.5-turbo-0125",
                Messages = new[] { systemMessage, userMessage },
                Functions = new[] { _ideaFunction },
                FunctionCall = new { Name = _ideaFunction.Name }
            };

            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);
            ChatResponse? response = await httpResponse.Content.ReadFromJsonAsync<ChatResponse>();

            //Get the first message in the function
            ChatFunctionResponse? functionResponse = response.Choices.
                FirstOrDefault(m => m.Message?.FunctionCall is not null)?
                .Message?
                .FunctionCall;
            Result<List<Idea>> ideasResult = new();

            if (functionResponse?.Arguments is not null)
            {
                try
                {
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
        
    }
}
