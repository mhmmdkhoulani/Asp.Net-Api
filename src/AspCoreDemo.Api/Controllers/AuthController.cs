using AspCoreDemo.Api.Models;
using AspCoreDemo.Api.Services;
using AspCoreDemo.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspCoreDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private IUserService _userService;
        private IMailService _mailService;
        private IConfiguration _configuration;
        public AuthController(IUserService userService, IMailService mailService, IConfiguration configuration)
        {
            _userService = userService;
            _mailService = mailService;
            _configuration = configuration;
        }

        // /api/auth/register
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterAsync([FromBody]RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.RegisterUserAsync(model);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }

            return BadRequest("Some properties are not valid");// return code 400
        }


        // /api/auth/login
        [HttpPost("Login")]
        
        public async Task<IActionResult> LoginAsync([FromBody]LoginViewModel model)
        {
            if (ModelState.IsValid)
            {

                var resutl = await _userService.LoginUserAsync(model);
                if (resutl.IsSuccess)
                {
                    await _mailService.SendEmailAsync(model.Email, "test email", "<h1>Everything is working fine now</h1>");
                    return Ok(resutl);
                }
                return BadRequest(resutl);
            }

            return BadRequest("Some properties are not valid");
        }

        // /api/auth/confirmemail?userId&token
        [HttpGet("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token)) 
            {
                return NotFound();
            }

            var result = await _userService.ConfirmEmailAsync(userId, token);

            if (result.IsSuccess)
            {
                return Redirect($"{_configuration["AppUrl"]}/confirmemail.html");

            }
            return BadRequest(result);
        }

        // /api/auth/forgerpassword
        [HttpPost("forgetpassword")] 
        public async Task<IActionResult> ForgetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return NotFound();
            }

            var result = await _userService.ForgetPasswordAsync(email);

            if (result.IsSuccess) 
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.ResetPasswordAsync(model);
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            return BadRequest("Some properties are not valid");
        }

    }
}
