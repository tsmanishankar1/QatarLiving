using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.ServiceInterface;

namespace QLN.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _service;

        public UserProfileController(IUserProfileService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserProfileCreateRequest request)
        {
            try
            {
                var createdUser = await _service.AddUserProfileAsync(request);
                var response = new
                {
                    Success = true,
                    Message = createdUser
                };
                return Ok(response);
            }
            catch (MessageNotFoundException ex)
            {
                return ErrorClass.NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorClass.ErrorResponse(ex.Message);
            }
        }
    }
}
