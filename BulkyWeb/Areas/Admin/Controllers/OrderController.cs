﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";


            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        #region
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaderList = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment).ToList();
                    break;
                case "inprocess":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusInProcess).ToList();
                    break;
                case "completed":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusShipped).ToList();
                    break;
                case "approved":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.PaymentStatus == SD.StatusApproved).ToList();
                    break;
                default:
                    break;

            }
            return Json(new { data = objOrderHeaderList });
        }

        #endregion
    }
}
