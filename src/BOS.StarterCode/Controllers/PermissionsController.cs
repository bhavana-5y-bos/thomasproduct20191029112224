using BOS.IA.Client;
using BOS.IA.Client.ClientModels;
using BOS.StarterCode.Helpers;
using BOS.StarterCode.Models.BOSModels;
using BOS.StarterCode.Models.BOSModels.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace BOS.StarterCode.Controllers
{
    [Authorize(Policy = "IsAuthenticated")]
    public class PermissionsController : Controller
    {
        private readonly IIAClient _bosIAClient;

        public PermissionsController(IIAClient iaClient)
        {
            _bosIAClient = iaClient;
        }

        public IActionResult Index()
        {
            return View(GetPageData());
        }

        private dynamic GetPageData()
        {
            var modules = HttpContext.Session.GetObject<List<Module>>("Modules");
            dynamic model = new ExpandoObject();
            model.Modules = modules;
            if (User.FindFirst(c => c.Type == "Username") != null || User.FindFirst(c => c.Type == "Role") != null)
            {
                model.Username = User.FindFirst(c => c.Type == "Username").Value.ToString();
                model.Roles = User.FindFirst(c => c.Type == "Role").Value.ToString();
            }
            return model;
        }

        public async Task<ActionResult> FetchPermissions(string roleId, string roleName)
        {
            var model = GetPageData();
            var ownerPermissionsresponse = await _bosIAClient.GetOwnerPermissionsSetsAsFlatAsync<PermissionsModule>(Guid.Parse(roleId));

            List<Module> allModules = new List<Module>();
            List<Operation> allOperations = new List<Operation>();
            List<IPermissionsOperation> permittedOperations = new List<IPermissionsOperation>();
            List<IPermissionsSet> permittedModules = new List<IPermissionsSet>();

            if (ownerPermissionsresponse.IsSuccessStatusCode)
            {
                permittedModules = ownerPermissionsresponse.Permissions.Modules;
                permittedOperations = ownerPermissionsresponse.Permissions.Operations;
            }

            var modulesResponse = await _bosIAClient.GetModulesAsync<Module>(true, true);
            if (modulesResponse.IsSuccessStatusCode)
            {
                allModules = modulesResponse.Modules;
            }

            var operationsResponse = await _bosIAClient.GetOperationsAsync<Operation>(true, true);
            if (operationsResponse.IsSuccessStatusCode)
            {
                allOperations = operationsResponse.Operations;
            }

            foreach (PermissionsSet module in permittedModules)
            {
                var moduleObj = allModules.FirstOrDefault(x => x.Id == module.ModuleId);

                if (moduleObj != null)
                {
                    moduleObj.IsPermitted = true;
                    if (moduleObj.Operations.Count > 0)
                    {
                        foreach (Operation operation in moduleObj.Operations)
                        {
                            var operationObj = permittedOperations.FirstOrDefault(x => x.OperationId == operation.Id);
                            if (operationObj != null)
                            {
                                operation.IsPermitted = true;
                            }
                        }
                    }
                }
            }

            model.ModuleOperations = allModules;
            model.OwnerId = roleId;
            model.RoleName = roleName;
            return View("Index", model);
        }

        [HttpPost]
        public async Task<string> UpdatePermissions([FromBody] JObject data)
        {
            try
            {
                PermissionsModule permissionsModule = new PermissionsModule();
                List<PermissionsSet> modules = data["Modules"].ToObject<List<PermissionsSet>>();
                permissionsModule.Modules = new List<IPermissionsSet>();
                permissionsModule.Modules.AddRange(modules);

                List<PermissionsOperation> operations = data["Operations"].ToObject<List<PermissionsOperation>>();
                permissionsModule.Operations = new List<IPermissionsOperation>();
                permissionsModule.Operations.AddRange(operations);

                permissionsModule.OwnerId = Guid.Parse(data["OwnerId"].ToString());
                permissionsModule.Type = SetType.Role;

                var response = await _bosIAClient.AddPermissionsAsync<PermissionsModule>(permissionsModule);
                if (response.IsSuccessStatusCode)
                {
                    return "Permissions updated successfully";
                }
                else
                {
                    return response.BOSErrors[0].Message;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public IActionResult BackToRoles()
        {
            return RedirectToAction("Index", "Roles");
        }
    }
}