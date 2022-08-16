using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Backend.Controllers
{
    public class RecipeController
    {
        private static bool s_isLoaded = false;
        private static List<Recipe> s_recipes { get; set; } = new List<Recipe>();
        private static List<string> s_categoriesNames { get; set; } = new List<string>();
        [HttpGet]
        [Route("api/list-recipes"), Authorize]
        public List<Recipe> ListRecipes()
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            if (s_recipes.Count == 0)
                throw new InvalidOperationException("Cant be empty");
            else
                return s_recipes;
        }
        [HttpGet]
        [Route("api/list-categories"), Authorize]
        public List<string> ListCategories()
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            if (s_categoriesNames.Count == 0)
                throw new InvalidOperationException("Cant be empty");
            else
                return s_categoriesNames;
        }
        [HttpPost]
        [Route("api/add-category/{category}"), Authorize]
        public async void AddCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                s_categoriesNames.Add(category);
                string fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
                string jsonString = JsonSerializer.Serialize(s_categoriesNames);
                await File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpPost]
        [Route("api/add-recipe/{jsonRecipe}"), Authorize]
        public async void AddRecipe(string jsonRecipe)
        {
            Recipe recipe = JsonSerializer.Deserialize<Recipe>(jsonRecipe);
            recipe.Ingredients = recipe.Ingredients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            recipe.Instructions = recipe.Instructions.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            if (recipe.Ingredients.Count == 0 || recipe.Instructions.Count == 0 || recipe.Categories.Count == 0 || string.IsNullOrWhiteSpace(recipe.Title))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                s_recipes.Add(recipe);
                string fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                string jsonString = JsonSerializer.Serialize(s_recipes);
                await File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpDelete]
        [Route("api/delete-category/{category}"), Authorize]
        public async void DeleteCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                s_categoriesNames.Remove(category);
                foreach (Recipe recipe in s_recipes)
                {
                    if (recipe.Categories.Contains(category))
                        recipe.Categories.Remove(category);
                }
                string fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
                string jsonString = JsonSerializer.Serialize(s_categoriesNames);
                await File.WriteAllTextAsync(fileName, jsonString);
                fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                jsonString = JsonSerializer.Serialize(s_recipes);
                await File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpPut]
        [Route("api/update-category/{position}/{newCategory}"), Authorize]
        public async void UpdateCategory(string position, string newCategory)
        {
            if (string.IsNullOrEmpty(newCategory))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                foreach (Recipe recipe in s_recipes)
                {
                    if (recipe.Categories.Contains(s_categoriesNames[int.Parse(position) - 1]))
                    {
                        recipe.Categories[recipe.Categories.IndexOf(s_categoriesNames[int.Parse(position) - 1])] = newCategory;
                    }
                }
                s_categoriesNames[int.Parse(position) - 1] = newCategory;
                string fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
                string jsonString = JsonSerializer.Serialize(s_categoriesNames);
                await File.WriteAllTextAsync(fileName, jsonString);
                fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                jsonString = JsonSerializer.Serialize(s_recipes);
                await File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpDelete]
        [Route("api/delete-recipe/{id}"), Authorize]
        public async void DeleteRecipe(Guid id)
        {
            if (id == Guid.Empty)
                throw new InvalidOperationException("Cant be empty");
            else
            {
                Recipe recipe = s_recipes.FirstOrDefault(x => x.Id == id);
                s_recipes.Remove(recipe);
                string fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                var jsonString = JsonSerializer.Serialize(s_recipes);
                await File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpPut]
        [Route("api/update-recipe/{jsonRecipe}/{id}"), Authorize]
        public async void UpdateRecipe(string jsonRecipe, Guid id)
        {
            if (id == Guid.Empty || string.IsNullOrEmpty(jsonRecipe))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                Recipe oldRecipe = s_recipes.FirstOrDefault(x => x.Id == id);
                Recipe newRecipe = JsonSerializer.Deserialize<Recipe>(jsonRecipe);
                newRecipe.Ingredients = newRecipe.Ingredients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                newRecipe.Instructions = newRecipe.Instructions.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                if (newRecipe.Ingredients.Count == 0 || newRecipe.Instructions.Count == 0 || newRecipe.Categories.Count == 0 || string.IsNullOrWhiteSpace(newRecipe.Title))
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    oldRecipe.Title = newRecipe.Title;
                    oldRecipe.Categories = newRecipe.Categories;
                    oldRecipe.Ingredients = newRecipe.Ingredients;
                    oldRecipe.Instructions = newRecipe.Instructions;
                    var fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                    var jsonString = JsonSerializer.Serialize(s_recipes);
                    await File.WriteAllTextAsync(fileName, jsonString);
                    s_isLoaded = false;
                }
            }
        }
        [HttpGet]
        [Route("api/get-recipe/{id}"), Authorize]
        public Recipe GetRecipe(Guid id)
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            if (id == Guid.Empty)
                throw new InvalidOperationException("Cant be empty");
            else
            {
                var recipe = s_recipes.FirstOrDefault(x => x.Id == id);
                return recipe;
            }
        }
        public static string PathCombine(string path1, string path2)
        {
            if (Path.IsPathRooted(path2))
            {
                path2 = path2.TrimStart(Path.DirectorySeparatorChar);
                path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);
            }
            return Path.Combine(path1, path2);
        }
        private async void LoadData()
        {
            string fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
            string jsonString = await File.ReadAllTextAsync(fileName);
            s_recipes = JsonSerializer.Deserialize<List<Recipe>>(jsonString);
            fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
            jsonString = await File.ReadAllTextAsync(fileName);
            s_categoriesNames = JsonSerializer.Deserialize<List<string>>(jsonString);
        }
    }
}
