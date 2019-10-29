using BOS.Auth.Client;
using BOS.Auth.Client.ClientModels;
using BOS.IA.Client;
using BOS.StarterCode.Helpers;
using BOS.StarterCode.Models;
using BOS.StarterCode.Models.BOSModels;
using BOS.StarterCode.Models.BOSModels.Permissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BOS.StarterCode.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthClient _bosAuthClient;
        private readonly IIAClient _bosIAClient;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthClient authClient, IIAClient iaClient, IConfiguration configuration)
        {
            _bosAuthClient = authClient;
            _bosIAClient = iaClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: Returns the "Login" view - The landing page of the application
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            try
            {
                //Check if user is authenticated then redirect him to clients
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {

            }
            return View("Index");
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: Triggers when the Login button is clicked
        /// </summary>
        /// <param name="authObj"></param>
        /// <returns></returns>
        public async Task<ActionResult> AuthenticateUser(AuthModel authObj)
        {
            if (!HttpContext.Request.Cookies.ContainsKey(".AspNet.Consent"))
            {
                ModelState.AddModelError("CustomError", "Before procedding, please 'Accept' our Cookies' terms.");
                return View("Index", new AuthModel());
            }
            if (authObj != null)
            {
                var result = await _bosAuthClient.SignInAsync(authObj.Username, authObj.Password);
                if (result.IsVerified)
                {

                    var userRoles = await _bosAuthClient.GetUserByIdWithRolesAsync<User>(result.UserId.Value);
                    var user = userRoles.User;
                    var roles = user.Roles;

                    // Convert Roles Array into a comma separated string containing roles
                    string rolesString = string.Empty;
                    if (roles != null && roles.Count > 0)
                    {
                        foreach (UserRole userRole in roles)
                        {
                            RoleUser role = userRole.Role;
                            rolesString = (!string.IsNullOrEmpty(rolesString)) ? (rolesString + "," + role.Name) : (role.Name);
                        }
                    }

                    //Create Claim Identity.
                    var claims = new List<Claim>{ new Claim("CreatedOn", DateTime.UtcNow.ToString()),
                                              new Claim("Email", user.Email),
                                              new Claim("Role", rolesString),
                                              new Claim("UserId", user.Id.ToString()),
                                              new Claim("Username", user.Username.ToString()),
                                              new Claim("IsAuthenticated", "True")
                                            };
                    var userIdentity = new ClaimsIdentity(claims, "Auth");
                    ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);

                    //Sign In created claims Identity Principal with cookie Authentication scheme
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(3000),
                        IsPersistent = false,
                        AllowRefresh = false
                    });

                    //Getting the permissions
                    var permissionSet = await _bosIAClient.GetOwnerPermissionsSetsAsync<Permissions>(result.UserId.Value);
                    if (permissionSet.IsSuccessStatusCode)
                    {
                        //Set Permissions in Sessions
                        HttpContext.Session.SetObject("ModuleOperations", permissionSet.Permissions.Modules);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("CustomError", "Username or password is incorrect");
                    return View("Index", new AuthModel());
                }
            }
            else
            {
                return View("Index", new AuthModel());
            }
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: When clicked on the 'Register' link on the page, navigates to the 'Register view
        /// </summary>
        /// <returns></returns>
        public ActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: Triggers when the Register button is clicked
        /// </summary>
        /// <param name="registerObj"></param>
        /// <returns></returns>
        public async Task<ActionResult> RegisterUser(RegistrationModel registerObj)
        {
            var result = await _bosAuthClient.AddNewUserAsync<BOSUser>(registerObj.EmailAddress, registerObj.EmailAddress, CreatePassword());
            if (result.IsSuccessStatusCode)
            {
                User user = new User
                {
                    Id = result.User.Id,
                    CreatedOn = DateTime.UtcNow,
                    Deleted = false,
                    Email = registerObj.EmailAddress,
                    FirstName = registerObj.FirstName,
                    LastModifiedOn = DateTime.UtcNow,
                    LastName = registerObj.LastName,
                    Username = registerObj.EmailAddress
                };

                var extendUserResponse = await _bosAuthClient.ExtendUserAsync(user);
                if (extendUserResponse.IsSuccessStatusCode)
                {
                    List<Role> roleList = new List<Role>();

                    var availableRoles = await _bosAuthClient.GetRolesAsync<Role>();
                    if (availableRoles.IsSuccessStatusCode)
                    {
                        Role defaultRole = availableRoles.Roles.FirstOrDefault(i => i.Name == "User");
                        roleList.Add(defaultRole);
                        var roleResponse = await _bosAuthClient.AssociateUserToMultipleRolesAsync(result.User.Id, roleList);
                        if (roleResponse.IsSuccessStatusCode)
                        {
                            var slugResponse = await _bosAuthClient.CreateSlugAsync(registerObj.EmailAddress);
                            if (slugResponse.IsSuccessStatusCode)
                            {
                                var slug = slugResponse.Slug;

                                ViewBag.Message = "Welcome! You've been successfully registered with us. Login in with your credentials to start using our product.";

                                EmailHelper helper = new EmailHelper(_configuration["SendGrid:From"], _configuration["SendGrid:ApiKey"]);
                                Dictionary<string, string> parameters = new Dictionary<string, string>();
                                parameters.Add("Name", registerObj.FirstName + " " + registerObj.LastName);
                                parameters.Add("From", "startercode@bosframework.com");
                                parameters.Add("Subject", "Welcome to the Starter Code");
                                parameters.Add("Action", "Registration");
                                parameters.Add("ApplicationURL", _configuration["PublicUrl"]);
                                parameters.Add("SlugValue", slug.Value);
                                parameters.Add("SlugExpiration", slug.ExpirationDate.ToString());
                                await helper.SendEmail(registerObj.EmailAddress, parameters);

                                return View("Index");

                            }
                        }
                    }
                }
                ModelState.AddModelError("CustomError", result.BOSErrors[0].Message);
                return View("Register");
            }
            else
            {
                ModelState.AddModelError("CustomError", result.BOSErrors[0].Message);
                return View("Register");
            }
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: When clicked on the 'Register' link on the page, navigates to the 'Register view
        /// </summary>
        /// <returns></returns>
        public ActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Author: BOS Framework, Inc
        /// Description: Triggers when the Register button is clicked
        /// </summary>
        /// <param name="forgotPasswordObj"></param>
        /// <returns></returns>
        public async Task<ActionResult> ForgotPasswordAction(ForgotPassword forgotPasswordObj)
        {
            if (ModelState.IsValid)
            {
                var userResponse = await _bosAuthClient.GetUserByEmailAsync<BOSUser>(forgotPasswordObj.EmailAddress);

                if (userResponse.Users != null)
                {
                    var slugResponse = await _bosAuthClient.CreateSlugAsync(forgotPasswordObj.EmailAddress);
                    if (slugResponse.IsSuccessStatusCode)
                    {
                        var slug = slugResponse.Slug;
                        EmailHelper helper = new EmailHelper(_configuration["SendGrid:From"], _configuration["SendGrid:ApiKey"]);
                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        parameters.Add("From", "startercode@bosframework.com");
                        parameters.Add("Subject", "Forgot password request");
                        parameters.Add("Action", "ForgotPassword");
                        parameters.Add("Password", "tempPassword");
                        parameters.Add("ApplicationURL", _configuration["PublicUrl"]);
                        parameters.Add("SlugValue", slug.Value);
                        parameters.Add("SlugExpiration", slug.ExpirationDate.ToString());
                        await helper.SendEmail(forgotPasswordObj.EmailAddress, parameters);
                    }
                }
            }
            ViewBag.Message = "Check your inbox for an email with a link to reset your password.";
            return View("Index");
        }

        [HttpPost]
        public async Task<string> ForcePasswordChange([FromBody]JObject data)
        {
            StringConversion stringConversion = new StringConversion();
            string userId = stringConversion.DecryptString(data["userId"].ToString());

            string password = data["password"].ToString();
            var response = await _bosAuthClient.ForcePasswordChangeAsync(Guid.Parse(userId), password);
            if (response.IsSuccessStatusCode)
            {
                return "Password updated successfully";
            }
            else
            {
                throw new Exception(response.BOSErrors[0].Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return GoToReturnUrl("/Auth");
            }
            catch (Exception ex)
            {

            }
            return View();
        }

        private IActionResult GoToReturnUrl(string returnUrl)
        {
            try
            {
                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error occurred in GoToReturnUrl Action of AccountController.");
            }
            return RedirectToAction("Index", "Clients");
        }

        private string CreatePassword()
        {
            int length = 10;
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&*()";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
    }
}
