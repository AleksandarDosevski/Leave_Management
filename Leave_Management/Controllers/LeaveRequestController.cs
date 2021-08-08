using AutoMapper;
using Leave_Management.Contracts;
using Leave_Management.Data;
using Leave_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly ILeaveAllocationRepository _leaveAllocRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(
            ILeaveRequestRepository leaveRequestRepo, 
            ILeaveTypeRepository leaveTypeRepo, 
            ILeaveAllocationRepository leaveAllocRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper, 
            UserManager<Employee> userManager
            )
        {
            _leaveRequestRepo = leaveRequestRepo;
            _leaveTypeRepo = leaveTypeRepo;
            _leaveAllocRepo = leaveAllocRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        // GET: LeaveRequestController
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> Index()
        {
            var leaveRequests = await _unitOfWork.LeaveRequests.FindAll(
                includes: new List<string> {"RequestingEmployee", "LeaveType"});
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests.ToList());
            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequestsModel.Count,
                ApprovedRequests = leaveRequestsModel.Count(q => q.Approved == true),
                PendingRequests = leaveRequestsModel.Count(q => q.Approved == null && q.Cancelled != true),
                CancelledRequests = leaveRequestsModel.Count(q => q.Cancelled == true),
                RejectedRequests = leaveRequestsModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequestsModel
            };
            return View(model);
        }

        public async Task<ActionResult> MyLeave()
        {
            var employee = await _userManager.GetUserAsync(User);
            var employeeid = employee.Id;

           // var employeeAllocations = await _leaveAllocRepo.GetLeaveAllocationsByEmployee(employeeid);
            var employeeAllocations = await _unitOfWork.LeaveAllocations.FindAll(q => q.EmployeeId == employeeid, 
                includes: new List<string> {"LeaveType"});

           // var employeeRequests = await _leaveRequestRepo.GetLeaveRequestsByEmployee(employeeid);
            var employeeRequests = await _unitOfWork.LeaveRequests.FindAll(q => q.RequestingEmployeeId == employeeid);

            var employeeAllocationsModel = _mapper.Map<List<LeaveAllocationVM>>(employeeAllocations);
            var employeeRequestsModel = _mapper.Map<List<LeaveRequestVM>>(employeeRequests);

            var model = new EmployeeLeaveRequestViewVM
            {
                LeaveAllocations = employeeAllocationsModel,
                LeaveRequests = employeeRequestsModel
            };

            return View(model);
        }


        // GET: LeaveRequestController/Details/5
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> Details(int id)
        {
           // var leaveRequest = await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id,
                includes: new List<string> { "ApprovedBy", "RequestingEmployee", "LeaveType"});
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);
            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ApproveRequest(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                // var leaveRequest = await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);
                var employeeid = leaveRequest.RequestingEmployeeId;
                var leaveTypeId = leaveRequest.LeaveTypeId;
                var period = DateTime.Now.Year;

               // var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeId);
                var allocation = await _unitOfWork.LeaveAllocations.Find(q => q.EmployeeId == employeeid
                                                && q.Period == period
                                                && q.LeaveTypeId == leaveTypeId);

                var daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
                allocation.NumberOfDays -= daysRequested;

                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

             //   var isSuccess = _leaveRequestRepo.Update(leaveRequest);

                //await _leaveRequestRepo.Update(leaveRequest);
                //await _leaveAllocRepo.Update(allocation);

                _unitOfWork.LeaveRequests.Update(leaveRequest);
                _unitOfWork.LeaveAllocations.Update(allocation);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> RejectRequest(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
               // var leaveRequest = await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);
                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaveRequestRepo.Update(leaveRequest);                
                _unitOfWork.LeaveRequests.Update(leaveRequest);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index), "Home");

            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index), "Home");
            }
        }


        // GET: LeaveRequestController/Create
        public async Task<ActionResult> Create()
        {
           // var leaveTypes = await _leaveTypeRepo.FindAll();
            var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();

            var leaveTypeItems = leaveTypes.Select(q => new SelectListItem 
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });
            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequestController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLeaveRequestVM model)
        {
            try
            {
                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);
               // var leaveTypes = await _leaveTypeRepo.FindAll();
                var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();
                var employee = await _userManager.GetUserAsync(User);
                var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });
                model.LeaveTypes = leaveTypeItems;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (DateTime.Compare(startDate, endDate) > 1)
                {
                    ModelState.AddModelError("", "Start Date must be before the End Date"); 
                    return View(model);
                }

                var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);
                int daysRequested = (int)(endDate.Date - startDate.Date).TotalDays;

                if (daysRequested > allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You do not have sufficien days for this request");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    LeaveTypeId = model.LeaveTypeId,
                    RequestComments = model.RequestComments
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                //  var isSuccess = await _leaveRequestRepo.Create(leaveRequest);
                await _unitOfWork.LeaveRequests.Create(leaveRequest);
                await _unitOfWork.Save();

                //if (!isSuccess)
                //{
                //    ModelState.AddModelError("", "Something went wrong with submitting your record");
                //    return View(model);
                //}

                return RedirectToAction("MyLeave");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }
        }

        public async Task<ActionResult> CancelRequest(int id)
        {
           // var leaveRequest = await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);
            leaveRequest.Cancelled = true;
            //_leaveRequestRepo.Delete(leaveRequest);
            //var employeeid = leaveRequest.RequestingEmployeeId;
            //var leaveTypeId = leaveRequest.LeaveTypeId;
            //var allocation = _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeId);
            //var daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
            //if (leaveRequest.Approved == true)
            //{
            //    allocation.NumberOfDays += daysRequested;
            //}
            //_leaveAllocRepo.Update(allocation);
            //await _leaveRequestRepo.Update(leaveRequest);
            _unitOfWork.LeaveRequests.Update(leaveRequest);
            await _unitOfWork.Save();   
            return RedirectToAction("MyLeave");
        }

        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> CancelApprovedRequest(int id)
        {
            var leaveRequest = await _leaveRequestRepo.FindById(id);
            leaveRequest.Cancelled = true;
            
            //_leaveRequestRepo.Delete(leaveRequest);
            var employeeid = leaveRequest.RequestingEmployeeId;
            var leaveTypeId = leaveRequest.LeaveTypeId;
            var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeId);
            var daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
            if (leaveRequest.Approved == true)
            {
                allocation.NumberOfDays += daysRequested;
                leaveRequest.Approved = null;
            }
            await _leaveAllocRepo.Update(allocation);
            return RedirectToAction(nameof(Index));
        }


        // GET: LeaveRequestController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveRequestController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }

    }
}
