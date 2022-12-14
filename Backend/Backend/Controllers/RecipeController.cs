using Backend.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Backend.Controllers
{
    public class RecipeController : Controller
    {
        private static bool s_isLoaded = false;
        private static List<Recipe> s_recipes { get; set; } = new List<Recipe>();
        private static List<string> s_categoriesNames { get; set; } = new List<string>();
        private readonly IAntiforgery _antiforgery;
        public static IAntiforgery GlobalAntiforgery;
        public RecipeController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        [HttpGet]
        [Route("api/list-recipes"), Authorize]
        public async Task<ActionResult<Recipe>> ListRecipes()
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (!s_isLoaded)
                {
                    await LoadData();
                }
                if (s_recipes.Count == 0)
                    throw new InvalidOperationException("Cant be empty");
                else
                    return Ok(s_recipes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("api/list-categories"), Authorize]
        public async Task<ActionResult<string>> ListCategories()
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (!s_isLoaded)
                {
                    await LoadData();
                }
                if (s_categoriesNames.Count == 0)
                    throw new InvalidOperationException("Cant be empty");
                else
                    return Ok(s_categoriesNames);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("api/add-category"), Authorize]
        public async Task<ActionResult> AddCategory([FromBody] string category)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (string.IsNullOrEmpty(category))
                    return BadRequest("Cant be empty");
                else
                {
                    s_categoriesNames.Add(category);
                    await SaveCategories();
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        [Route("api/add-recipe"), Authorize]
        public async Task<ActionResult> AddRecipe([FromBody] Recipe recipe)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                recipe.Ingredients = recipe.Ingredients.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                recipe.Instructions = recipe.Instructions.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
                if (recipe.Ingredients.Count == 0 || recipe.Instructions.Count == 0 || recipe.Categories.Count == 0 || string.IsNullOrWhiteSpace(recipe.Title))
                    return BadRequest("Cant be empty");
                else
                {
                    s_recipes.Add(recipe);
                    await SaveRecipes();
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [Route("api/delete-category"), Authorize]
        public async Task<ActionResult> DeleteCategory([FromBody] string category)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (string.IsNullOrEmpty(category))
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    if (!s_isLoaded)
                    {
                        await LoadData();
                    }
                    s_categoriesNames.Remove(category);
                    foreach (Recipe recipe in s_recipes)
                    {
                        if (recipe.Categories.Contains(category))
                            recipe.Categories.Remove(category);
                    }
                    await SaveCategories();
                    await SaveRecipes();
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut]
        [Route("api/update-category/{position}"), Authorize]
        public async Task<ActionResult> UpdateCategory(string position, [FromBody] string newCategory)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (string.IsNullOrEmpty(newCategory))
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    if (!s_isLoaded)
                    {
                        await LoadData();
                    }
                    foreach (Recipe recipe in s_recipes)
                    {
                        if (recipe.Categories.Contains(s_categoriesNames[int.Parse(position) - 1]))
                        {
                            recipe.Categories[recipe.Categories.IndexOf(s_categoriesNames[int.Parse(position) - 1])] = newCategory;
                        }
                    }
                    s_categoriesNames[int.Parse(position) - 1] = newCategory;
                    await SaveCategories();
                    await SaveRecipes();
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [Route("api/delete-recipe"), Authorize]
        public async Task<ActionResult> DeleteRecipe([FromBody] Guid id)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (id == Guid.Empty)
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    if (!s_isLoaded)
                    {
                        await LoadData();
                    }
                    Recipe recipe = s_recipes.FirstOrDefault(x => x.Id == id);
                    s_recipes.Remove(recipe);
                    await SaveRecipes();
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut]
        [Route("api/update-recipe/{id}"), Authorize]
        public async Task<ActionResult> UpdateRecipe([FromBody] Recipe newRecipe, Guid id)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (id == Guid.Empty || newRecipe.Ingredients.Count == 0 || newRecipe.Instructions.Count == 0 || newRecipe.Categories.Count == 0 || string.IsNullOrWhiteSpace(newRecipe.Title))
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    Recipe oldRecipe = s_recipes.FirstOrDefault(x => x.Id == id);
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
                        await SaveRecipes();
                        return Ok();
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("api/get-recipe/{id}"), Authorize]
        public async Task<ActionResult<Recipe>> GetRecipe(Guid id)
        {
            try
            {
                await GlobalAntiforgery.ValidateRequestAsync(HttpContext);
                if (!s_isLoaded)
                {
                    await LoadData();
                }
                if (id == Guid.Empty)
                    throw new InvalidOperationException("Cant be empty");
                else
                {
                    var recipe = s_recipes.FirstOrDefault(x => x.Id == id);
                    return Ok(recipe);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("api/antiforgery"), Authorize]
        public void GetAntiforgery()
        {
            GlobalAntiforgery = _antiforgery;
            var tokens = GlobalAntiforgery.GetAndStoreTokens(HttpContext);
            HttpContext.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.None, Secure = true });
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

        private async Task LoadData()
        {
            while (!s_isLoaded)
            {
                try
                {
                    string fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                    string jsonString = await System.IO.File.ReadAllTextAsync(fileName);
                    s_recipes = JsonSerializer.Deserialize<List<Recipe>>(jsonString);
                    fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
                    jsonString = await System.IO.File.ReadAllTextAsync(fileName);
                    s_categoriesNames = JsonSerializer.Deserialize<List<string>>(jsonString);
                    s_isLoaded = true;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        private async Task SaveRecipes()
        {
            while (true)
            {
                try
                {
                    SortRecipes();
                    var fileName = PathCombine(Environment.CurrentDirectory, @"\Recipes.json");
                    var jsonString = JsonSerializer.Serialize(s_recipes);
                    await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                    s_isLoaded = false;
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        private async Task SaveCategories()
        {
            while (true)
            {
                try
                {
                    SortCategories();
                    string fileName = PathCombine(Environment.CurrentDirectory, @"\Categories.json");
                    string jsonString = JsonSerializer.Serialize(s_categoriesNames);
                    await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                    s_isLoaded = false;
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void SortCategories()
        {
            int x = 0;
            do
            {
                x = 0;
                for (int i = 0; i < s_categoriesNames.Count - 1; i++)
                {
                    if (char.ToUpper(s_categoriesNames[i][0]) > char.ToUpper(s_categoriesNames[i + 1][0]))
                    {
                        x++;
                        string tmp = s_categoriesNames[i];
                        s_categoriesNames[i] = s_categoriesNames[i + 1];
                        s_categoriesNames[i + 1] = tmp;
                    }
                }

            } while (x > 0);
        }

        private void SortRecipes()
        {
            int x = 0;
            do
            {
                x = 0;
                for (int i = 0; i < s_recipes.Count - 1; i++)
                {
                    if (char.ToUpper(s_recipes[i].Title[0]) > char.ToUpper(s_recipes[i + 1].Title[0]))
                    {
                        x++;
                        Recipe tmp = s_recipes[i];
                        s_recipes[i] = s_recipes[i + 1];
                        s_recipes[i + 1] = tmp;
                    }
                }

            } while (x > 0);
        }
    }
}
