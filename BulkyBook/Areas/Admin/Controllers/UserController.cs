﻿using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        public UserController(UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagment(string userId)
        {
            var roleVM = new RoleManagmentVM()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId,includeProperties:"Company"),
                RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };
            roleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
                .GetAwaiter().GetResult().FirstOrDefault();
            return View(roleVM);
        }

        [HttpPost]
        public IActionResult RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVM.ApplicationUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVM.ApplicationUser.Id);
            if (!(roleManagmentVM.ApplicationUser.Role == oldRole))
            {
                if(roleManagmentVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(oldRole == SD.Role_Company && applicationUser.CompanyId != roleManagmentVM.ApplicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties:"Company").ToList();
            foreach (var user in objUserList)
            {
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
                if(user.Company == null)
                {
                    user.Company = new Company() { Name = "" };
                }
            }
            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false,message = "Error while Locking/Unlocking" });
            }

            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion
    }
}
