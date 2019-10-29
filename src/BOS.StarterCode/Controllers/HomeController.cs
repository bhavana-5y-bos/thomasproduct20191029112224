using BOS.StarterCode.Helpers;
using BOS.StarterCode.Models;
using BOS.StarterCode.Models.BOSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;

namespace BOS.StarterCode.Controllers
{
    [Authorize(Policy = "IsAuthenticated")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(GetPageData());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult NavigationMenu(string selectedModuleId)
        {
            var model = GetPageData();
            model.CurrentModuleId = selectedModuleId;
            return View("Index", model);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private dynamic GetPageData()
        {
            var modules = HttpContext.Session.GetObject<List<Module>>("Modules");
            dynamic model = new ExpandoObject();
            model.Modules = modules;
            model.Username = User.FindFirst(c => c.Type == "Username").Value.ToString();
            model.Roles = User.FindFirst(c => c.Type == "Role").Value.ToString();
            model.ModuleOperations = HttpContext.Session.GetObject<List<Module>>("ModuleOperations");
            model.CurrentModuleId = null;
            return model;
        }
    }
}
