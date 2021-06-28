using AspCoreDemo.Api.Models;
using AspCoreDemo.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AspCoreDemo.Api.Services
{
    public interface IUserService
    {

        Task<UserManagerResponse> RegisterUserAsync(RegisterViewModel model);
        Task<UserManagerResponse> LoginUserAsync(LoginViewModel model);
        Task<UserManagerResponse> ConfirmEmailAsync(string userId, string token);
        Task<UserManagerResponse> ForgetPasswordAsync(string email);
        Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model);

    }

    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;
        private IConfiguration _configration;
        private IMailService _mailService;

       

        public UserService(UserManager<IdentityUser> userManager, IConfiguration configration, IMailService mailService)
        {
            _userManager = userManager;
            _configration = configration;
            _mailService = mailService;
        }

        

        public async Task<UserManagerResponse> RegisterUserAsync(RegisterViewModel model)
        {
            if (model == null)
            {
                throw new NullReferenceException("Register Model is null");
            }

            if (model.Password != model.ConfirmPassword)
            {
                return new UserManagerResponse
                {
                    Message = "Confirm password does not match the password",
                    IsSuccess = false
                };
            }

            var IdentityUser = new IdentityUser
            {
                Email = model.Email,
                UserName = model.Email
            };

            var result = await _userManager.CreateAsync(IdentityUser, model.Password);

            if (result.Succeeded)
            {


                var confirmEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(IdentityUser);
                var encodedEmailToken = Encoding.UTF8.GetBytes(confirmEmailToken);
                var validEmailToken = WebEncoders.Base64UrlEncode(encodedEmailToken);

                string url = $"{_configration["AppUrl"]}/api/auth/confirmEmail?userId={IdentityUser.Id}&token={validEmailToken}";

                await _mailService.SendEmailAsync(IdentityUser.Email, "Email Confirmation", "<h1>Welcome to Demo</h1>" +
                    $"<p>Please confirm your email by <a href='{url}'>Clicking here</a></p>");

                return new UserManagerResponse
                {
                    Message = "User created successfully",
                    IsSuccess = true,
                };
            }
            return new UserManagerResponse
            {
                Message = "User didnt create",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        public async Task<UserManagerResponse> LoginUserAsync(LoginViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message = "There is no user with this email address",
                    IsSuccess = false
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!result)
            {
                return new UserManagerResponse
                {
                    Message = "Password is not correct",
                    IsSuccess = false
                };

            }

            var claims = new[]
            {
                new Claim("Email", model.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configration["AuthSettings:Key"]));

            var token = new JwtSecurityToken(
                issuer: _configration["AuthSettings: Issuer"],
                audience: _configration["AuthSettings: Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials : new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            string tokeAsString = new JwtSecurityTokenHandler().WriteToken(token);

            return new UserManagerResponse
            {
                Message = tokeAsString,
                IsSuccess = true,
                ExpireDate = token.ValidTo
            };
        }

        public async Task<UserManagerResponse> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message = "user not found",
                    IsSuccess = false
                };
            }

            var decodedToken = WebEncoders.Base64UrlDecode(token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);

            var result = await _userManager.ConfirmEmailAsync(user, normalToken);

            if (result.Succeeded)
            {
                return new UserManagerResponse
                {
                    Message = " Email Confirmed successfully",
                    IsSuccess = true
                };
            }

            return new UserManagerResponse
            {
                Message = "Email did not confirm",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };

        }

        public async Task<UserManagerResponse> ForgetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false
                };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedForgerPasswordToken = Encoding.UTF8.GetBytes(token);
            var validForgerPasswordToken = WebEncoders.Base64UrlEncode(encodedForgerPasswordToken);

            string url = $"{_configration["AppUrl"]}/resetpassword?email={email}&token={validForgerPasswordToken}";

            await _mailService.SendEmailAsync(email, "Reset Password", "<h1>Welcome to Demo</h1>" +
                   $"<p>You can reset your password by <a href='{url}'>Clicking here</a></p>");

            return new UserManagerResponse
            {
                IsSuccess = true,
                Message = "Resetn password url has been set to the email successfully",
            };
        }

        public async Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false
                };
            }

            if (model.NewPassword != model.ConfirmPassword) 
            {
                return new UserManagerResponse
                {
                    Message = "Confirm password does not match the password",
                    IsSuccess = false
                };
            }
            var decodedToken = WebEncoders.Base64UrlDecode(model.Token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);
            var result = await _userManager.ResetPasswordAsync(user, normalToken, model.NewPassword);

            if (result.Succeeded)
            {
                return new UserManagerResponse
                {
                    IsSuccess = true,
                    Message = "your password has been reset",
                };
            }
            return new UserManagerResponse
            {
                Message = "Somthing went wrong",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }


    }
}

