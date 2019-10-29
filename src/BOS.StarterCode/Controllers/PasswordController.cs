using BOS.Auth.Client;
using BOS.StarterCode.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BOS.StarterCode.Controllers
{
    public class PasswordController : Controller
    {
        private readonly IAuthClient _bosAuthClient;

        public PasswordController(IAuthClient authClient)
        {
            _bosAuthClient = authClient;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Reset(string slug)
        {
            var result = await _bosAuthClient.VerifySlugAsync(slug);
            if (result.IsSuccessStatusCode)
            {
                ViewBag.UserId = result.UserId;
            }
            else
            {
                ViewBag.Message = "The link has either expired or is invalid. Please try initiating the operation again.";
            }

            return View("ResetPassword");
        }

        public async Task<IActionResult> ResetPassword(ChangePassword password)
        {
            string userId = password.CurrentPassword;

            var response = await _bosAuthClient.ForcePasswordChangeAsync(Guid.Parse(password.CurrentPassword), password.NewPassword);
            if (response.IsSuccessStatusCode)
            {
                ViewBag.SuccessMessage = "Password reset successfully";
                return View();
            }
            else
            {
                throw new Exception(response.BOSErrors[0].Message);
            }
        }

        public IActionResult GotBackToLogin() {
            return RedirectToAction("Index", "Auth");
        }
    }
}