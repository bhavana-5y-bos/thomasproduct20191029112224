using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using BOS.StarterCode.Models.BOSModels;
using Microsoft.AspNetCore.Mvc;

namespace BOS.StarterCode.Controllers
{
    public class NavigationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult NavigateToModule(Guid id, string code, bool isDefault)
        {
            //if (isDefault)
            //{
                switch (code)
                {
                    case "MYPFL":
                        return RedirectToAction("Index", "Profile");
                    case "USERS":
                        return RedirectToAction("Index", "Users");
                    case "ROLES":
                        return RedirectToAction("Index", "Roles");
                    case "PRMNS":
                        return RedirectToAction("Index", "Permissions");
                    default:
                        return RedirectToAction("NavigationMenu", "Home", new { selectedModuleId = id});
                }
            //}
            //else
            //{
            //    ViewBag.ModuleSelected = "You've selected a custom module. Implement the logic to display the correct view";
            //    return RedirectToAction("Index", "Home");
            //}
        }
    }
}