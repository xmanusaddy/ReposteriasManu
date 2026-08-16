using Microsoft.AspNetCore.Mvc;
using ReposteriasManu.Application.Contract;
using ReposteriasManu.API.Responses;
using ReposteriasManu.Application.Dtos.Customer;
using ReposteriasManu.Domain.Entities;

namespace ReposteriasManu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _service.GetAllAsync();
            return Ok(customers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
                return NotFound(new ApiErrorResponse("Cliente no encontrado."));
            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiErrorResponse.FromModelState(ModelState));

            var customer = new Customer(dto.Name, dto.LastName, dto.Phone, dto.Email, dto.Address);
            await _service.AddAsync(customer);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiErrorResponse.FromModelState(ModelState));

            if (id != dto.Id)
                return BadRequest(new ApiErrorResponse("El identificador de la ruta no coincide con el cliente enviado."));

            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
                return NotFound(new ApiErrorResponse("Cliente no encontrado."));

            customer.Name = dto.Name;
            customer.LastName = dto.LastName;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            customer.Address = dto.Address;

            await _service.UpdateAsync(customer);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
                return NotFound(new ApiErrorResponse("Cliente no encontrado."));

            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
