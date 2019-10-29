using BOS.Auth.Client;
using BOS.Auth.Client.ClientModels;
using BOS.StarterCode.Helpers;
using BOS.StarterCode.Models;
using BOS.StarterCode.Models.BOSModels;
using BOS.StarterCode.Models.BOSModels.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace BOS.StarterCode.Controllers
{
    [Authorize(Policy = "IsAuthenticated")]
    public class UsersController : Controller
    {
        private readonly IAuthClient _bosAuthClient;

        public UsersController(IAuthClient authClient)
        {
            _bosAuthClient = authClient;
        }

        public async Task<IActionResult> Index()
        {
            return View(await GetPageData());
        }

        public IActionResult AddNewUser()
        {
            return View("AddUser");
        }

        public async Task<ActionResult> AddUser(RegistrationModel registerObj)
        {
            var result = await _bosAuthClient.AddNewUserAsync<BOSUser>(registerObj.EmailAddress, registerObj.EmailAddress, registerObj.Password);
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
                            ViewBag.Message = "User added successfully";
                            return View("Index", await GetPageData());
                        }
                    }
                }
                ModelState.AddModelError("CustomError", result.BOSErrors[0].Message);
                return View();
            }
            else
            {
                ModelState.AddModelError("CustomError", result.BOSErrors[0].Message);
                return View();
            }
        }

        public async Task<IActionResult> EditUser(string userId)
        {
            try
            {
                dynamic model = new ExpandoObject();
                StringConversion stringConversion = new StringConversion();
                string actualUserId = stringConversion.DecryptString(userId);
                var userInfo = await _bosAuthClient.GetUserByIdWithRolesAsync<User>(Guid.Parse(actualUserId));
                if (userInfo.IsSuccessStatusCode)
                {
                    userInfo.User.UpdatedId = userId;
                    model.UserInfo = userInfo.User;
                }

                List<string> rolesList = new List<string>();
                foreach (UserRole role in userInfo.User.Roles)
                {
                    rolesList.Add(role.Role.Name);
                }
                model.RolesList = rolesList;
                var availableRoles = await _bosAuthClient.GetRolesAsync<Role>();
                if (availableRoles.IsSuccessStatusCode)
                {
                    model.AvailableRoles = availableRoles.Roles;
                }

                return View("EditUser", model);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public async Task<string> DeleteUser([FromBody]string userId)
        {
            StringConversion stringConversion = new StringConversion();
            string actualUserId = stringConversion.DecryptString(userId);

            var response = await _bosAuthClient.DeleteUserAsync(Guid.Parse(actualUserId));
            if (response.IsSuccessStatusCode)
            {
                return "User deleted successfully";
            }
            else
            {
                throw new Exception(response.BOSErrors[0].Message);
            }
        }

        [HttpPost]
        public async Task<string> UpdateUserInfo([FromBody]User user)
        {
            StringConversion stringConversion = new StringConversion();
            user.Id = Guid.Parse(stringConversion.DecryptString(user.UpdatedId));
            var extendUserResponse = await _bosAuthClient.ExtendUserAsync(user);
            if (extendUserResponse.IsSuccessStatusCode)
            {
                return "User's information updated successfully";
            }
            else
            {
                return extendUserResponse.BOSErrors[0].Message;
            }
        }

        private async Task<dynamic> GetPageData()
        {
            var moduleOperations = HttpContext.Session.GetObject<List<Module>>("ModuleOperations");
            Guid currentModuleId = new Guid();
            try
            {
                currentModuleId = moduleOperations.Where(i => i.Code == "USERS").Select(i => i.Id).ToList()[0];
            }
            catch (ArgumentNullException)
            {
                currentModuleId = Guid.Empty;
            }

            var currentOperations = moduleOperations.Where(i => i.Id == currentModuleId).Select(i => i.Operations).ToList()[0];
            string operationsString = String.Join(",", currentOperations.Select(i => i.Code));

            dynamic model = new ExpandoObject();
            model.ModuleOperations = HttpContext.Session.GetObject<List<Module>>("ModuleOperations");
            model.Operations = operationsString;
            model.CurrentModuleId = currentModuleId;
            
            if (User.FindFirst(c => c.Type == "Username") != null || User.FindFirst(c => c.Type == "Role") != null)
            {
                model.Username = User.FindFirst(c => c.Type == "Username").Value.ToString();
                model.Roles = User.FindFirst(c => c.Type == "Role").Value.ToString();
            }

            StringConversion stringConversion = new StringConversion();
            var userList = await _bosAuthClient.GetUsersWithRolesAsync<User>();
            if (userList.IsSuccessStatusCode)
            {
                var updatedUserList = userList.Users.Select(c => { c.UpdatedId = stringConversion.EncryptString(c.Id.ToString()); return c; }).ToList();
                model.UserList = updatedUserList;
            }
            return model;
        }

    }
}

