using Microsoft.AspNetCore.Mvc;
using ProductAPIRedisCache.Application.Interfaces;
using ProductAPIRedisCache.Common;
using ProductAPIRedisCache.Domain.Entities;

namespace ProductAPIRedisCache.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService service, ILogger<ProductController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            //throw new InvalidOperationException("Test Exception: Controller Layer!");

            var products = await service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<Product>>.Ok(products, "Products retrieved successfully."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await service.GetByIdAsync(id);
            if (product is null)
                return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} was not found."));

            return Ok(ApiResponse<Product>.Ok(product, $"Product with ID {id} retrieved successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            var id = await service.CreateAsync(product);

            return CreatedAtAction(
                nameof(Get),
                new { id },
                ApiResponse<Product>.Ok(product, $"Product with ID {id} created successfully.")
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            product.Id = id;
            var result = await service.UpdateAsync(product);
            if (!result)
                return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} was not found."));

            return Ok(ApiResponse<bool>.Ok(true, $"Product with ID {id} updated successfully."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await service.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<Product>.Fail($"Product with ID {id} was not found."));

            return Ok(ApiResponse<bool>.Ok(true, $"Product with ID {id} deleted successfully."));
        }
    }

}
