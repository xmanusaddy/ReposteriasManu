using Microsoft.AspNetCore.Mvc;
using ReposteriasManu.Application.Contract;
using ReposteriasManu.Application.Dtos.Order;
using ReposteriasManu.Domain.Entities;

namespace ReposteriasManu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrderController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _service.GetAllAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _service.GetByIdAsync(id);
            if (order == null)
                return NotFound();
            return Ok(order);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var orders = await _service.GetByCustomerIdAsync(customerId);
            return Ok(orders);
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var orders = await _service.GetByStatusAsync(status);
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = new Order(
                DateTime.SpecifyKind(dto.OrderDate, DateTimeKind.Utc),
                DateTime.SpecifyKind(dto.DeliveryDate, DateTimeKind.Utc),
                dto.Status,
                dto.Notes,
                dto.CustomerId
            );
            await _service.AddAsync(order);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OrderUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest();

            var order = await _service.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            order.OrderDate = DateTime.SpecifyKind(dto.OrderDate, DateTimeKind.Utc);
            order.DeliveryDate = DateTime.SpecifyKind(dto.DeliveryDate, DateTimeKind.Utc);
            order.Status = dto.Status;
            order.Notes = dto.Notes;
            order.CustomerId = dto.CustomerId;

            await _service.UpdateAsync(order);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _service.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}