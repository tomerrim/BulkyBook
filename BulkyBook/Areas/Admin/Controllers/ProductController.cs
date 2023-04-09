﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        public IActionResult Index()
        {
            var objProductList = _unitOfWork.Product.GetAll().ToList();
            return View(objProductList);
        }
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll()
               .Select(c => new SelectListItem
               {
                   Text = c.Name,
                   Value = c.Id.ToString()
               }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else                      
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM,IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Add(productVM.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll()
               .Select(c => new SelectListItem
               {
                   Text = c.Name,
                   Value = c.Id.ToString()
               });
               return View(productVM);
            }
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            var productFromDb = _unitOfWork.Product.Get(c => c.Id == id);
            if (productFromDb == null)
                return NotFound();

            return View(productFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            var productFromDb = _unitOfWork.Product.Get(c => c.Id == id);
            if (productFromDb == null)
                return NotFound();

            _unitOfWork.Product.Remove(productFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
