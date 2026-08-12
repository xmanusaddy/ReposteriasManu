using Microsoft.AspNetCore.Mvc;
using ReposteriasManu.Application.Contract;
using ReposteriasManu.Application.Dtos.Decoration;
using ReposteriasManu.Domain.Entities;

namespace ReposteriasManu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecorationController : ControllerBase
    {
        private readonly IDecorationService _service;

        public DecorationController(IDecorationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var decorations = await _service.GetAllAsync();
            return Ok(decorations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var decoration = await _service.GetByIdAsync(id);
            if (decoration == null)
                return NotFound();
            return Ok(decoration);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var decorations = await _service.GetByOrderIdAsync(orderId);
            return Ok(decorations);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DecorationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var decoration = new Decoration(dto.Type, dto.Color, dto.Message, dto.OrderId, dto.ProductId);
            await _service.AddAsync(decoration);
            return CreatedAtAction(nameof(GetById), new { id = decoration.Id }, decoration);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DecorationUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest();

            var decoration = await _service.GetByIdAsync(id);
            if (decoration == null)
                return NotFound();

            decoration.Type = dto.Type;
            decoration.Color = dto.Color;
            decoration.Message = dto.Message;
            decoration.OrderId = dto.OrderId;
            decoration.ProductId = dto.ProductId;

            await _service.UpdateAsync(decoration);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var decoration = await _service.GetByIdAsync(id);
            if (decoration == null)
                return NotFound();

            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}