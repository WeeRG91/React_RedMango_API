using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Models.Dto;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace RedMango_API.Controllers
{
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;

        public MenuItemController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            try
            {
                var menuItems = await _db.MenuItems.ToListAsync();
                _response.Result = menuItems;
                _response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return Ok(_response);
        }
        
        [HttpGet("{id:int}", Name = "GetMenuItem")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }

            try
            {
                MenuItem menuItem = await _db.MenuItems.FirstOrDefaultAsync(item => item.Id == id);

                if (menuItem == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                _response.Result = menuItem;
                _response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return Ok(_response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDTO menuItemCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemCreateDTO.File == null || menuItemCreateDTO.File.Length == 0)
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages.Add("File is missing.");
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    // Generate a unique file name
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreateDTO.File.FileName)}";

                    // Define the path where the file will be saved
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);

                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                    // Save the file locally
                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        await menuItemCreateDTO.File.CopyToAsync(fileStream);
                    }

                    // Create the MenuItem object
                    MenuItem menuItemToCreate = new()
                    {
                        Name = menuItemCreateDTO.Name,
                        Price = menuItemCreateDTO.Price,
                        Category = menuItemCreateDTO.Category,
                        SpecialTag = menuItemCreateDTO.SpecialTag,
                        Description = menuItemCreateDTO.Description,
                        Image = $"/Images/{fileName}" // This will store the relative URL of the image
                    };

                    // Add the MenuItem to the database
                    _db.MenuItems.Add(menuItemToCreate);
                    await _db.SaveChangesAsync();

                    _response.Result = menuItemToCreate;
                    _response.StatusCode = HttpStatusCode.Created;
                    return CreatedAtRoute("GetMenuItem", new { id = menuItemToCreate.Id }, _response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid model state.");
                    return BadRequest(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
        
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDTO menuItemUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDTO == null || id != menuItemUpdateDTO.Id)
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages.Add("File is missing.");
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        return BadRequest(_response);
                    }

                    MenuItem menuItemFormDb = await _db.MenuItems.FindAsync(id);

                    if (menuItemFormDb == null)
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages.Add("Menu Item not found.");
                        return BadRequest(_response);
                    }

                    menuItemFormDb.Name = menuItemUpdateDTO.Name;
                    menuItemFormDb.Price = menuItemUpdateDTO.Price;
                    menuItemFormDb.Category = menuItemUpdateDTO.Category;
                    menuItemFormDb.SpecialTag = menuItemUpdateDTO.SpecialTag;
                    menuItemFormDb.Description = menuItemUpdateDTO.Description;

                    if (menuItemUpdateDTO.File != null && menuItemUpdateDTO.File.Length > 0)
                    {
                        // Delete the old image if it exists
                        if (!string.IsNullOrEmpty(menuItemFormDb.Image))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", menuItemFormDb.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);  // Delete the old image
                            }
                        }

                        // Generate a new unique file name for the new image
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDTO.File.FileName)}";
                        var newImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);

                        // Ensure the directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(newImagePath));

                        // Save the new file locally
                        using (var fileStream = new FileStream(newImagePath, FileMode.Create))
                        {
                            await menuItemUpdateDTO.File.CopyToAsync(fileStream);
                        }

                        // Update the image path in the database
                        menuItemFormDb.Image = $"/Images/{fileName}";
                    }
                   
                    // Update the MenuItem to the database
                    _db.MenuItems.Update(menuItemFormDb);
                    await _db.SaveChangesAsync();

                    _response.Result = menuItemFormDb;
                    _response.StatusCode = HttpStatusCode.OK;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid model state.");
                    return BadRequest(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid ID.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                MenuItem menuItemToDelete = await _db.MenuItems.FindAsync(id);

                if (menuItemToDelete == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Menu Item not found.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return BadRequest(_response);
                }
                
                // Delete the old image if it exists
                if (!string.IsNullOrEmpty(menuItemToDelete.Image))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", menuItemToDelete.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);  // Delete the old image
                    }
                }

                //int milliseconds = 2000;
                //Thread.Sleep(milliseconds);
                   
                // Delete the MenuItem to the database
                _db.MenuItems.Remove(menuItemToDelete);
                await _db.SaveChangesAsync();

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = $"Menu item with ID {id} was successfully deleted.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
