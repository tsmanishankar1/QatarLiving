using Microsoft.AspNetCore.Mvc;
using QLN.AIPOV.Backend.Application.Interfaces;
using QLN.AIPOV.Backend.Application.Models.Requests;

namespace QLN.AIPOV.Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(IChatService chatService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var response = await chatService.GetChatResponseAsync(request.Message);
            return Ok(new { message = response });
        }
    }
}
