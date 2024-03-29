﻿using AutoMapper;
using Leave_Management.Contracts;
using Leave_Management.Data;
using Leave_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class LeaveAllocationController : Controller
    {
        //private readonly ILeaveTypeRepository _leavetyperepo;
        //private readonly ILeaveAllocationRepository _leaveallocationrepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveAllocationController(
            //ILeaveTypeRepository leavetyperepo, 
            //ILeaveAllocationRepository leaveallocationrepo, 
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<Employee> userManager
        )
        {
            //_leavetyperepo = leavetyperepo;
            //_leaveallocationrepo = leaveallocationrepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }



        // GET: LeaveAllocationController
        public async Task<ActionResult> Index()
        {
            //var leavetypes = await _leavetyperepo.FindAll();
            var leavetypes = await _unitOfWork.LeaveTypes.FindAll();
            var mappedleaveTypes = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>(leavetypes.ToList());
            var model = new CreateLeaveAllocationVM
            {
                LeaveTypes = mappedleaveTypes,
                NumberUpdated = 0
            };
            return View(model);
        }

        public async Task<ActionResult> SetLeave (int id)
        {
            var leavetype = await _unitOfWork.LeaveTypes.Find(q => q.Id == id);
            var employees = _userManager.GetUsersInRoleAsync("Employee").Result;
            var period = DateTime.Now.Year;
            foreach (var emp in employees)
            {
               // if (await _leaveallocationrepo.CheckAllocation(id, emp.Id))
                if (await _unitOfWork.LeaveAllocations.isExists(q => q.Id == id
                                                                    && q.LeaveTypeId == id
                                                                    && q.Period == period))
                    continue;
                var allocation = new LeaveAllocationVM
                {       
                    DateCreated = DateTime.Now,
                    EmployeeId = emp.Id,
                    LeaveTypeId = id,
                    NumberOfDays = leavetype.DefaultDays,
                    Period = DateTime.Now.Year
                };
                var leaveallocation = _mapper.Map<LeaveAllocation>(allocation);
               // await _leaveallocationrepo.Create(leaveallocation);
                await _unitOfWork.LeaveAllocations.Create(leaveallocation);
                await _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
                                                                                                                                                            
        }

        public async Task<ActionResult> ListEmployees()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var model = _mapper.Map<List<EmployeeVM>>(employees);
            return View(model);
        }

        // GET: LeaveAllocationController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var employee = _mapper.Map<EmployeeVM>(await _userManager.FindByIdAsync(id));
            var period = DateTime.Now.Year;

            //var allocations = _mapper.Map<List<LeaveAllocationVM>>(await _leaveallocationrepo.GetLeaveAllocationsByEmployee(id));
            var records = await _unitOfWork.LeaveAllocations.FindAll(
                expression: q => q.EmployeeId == id && q.Period == period,
                includes: new List<string> { "LeaveType" });
            var allocations = _mapper.Map<List<LeaveAllocationVM>>(records);

            var model = new ViewAllocationsVM
            {
                Employee = employee,
                LeaveAllocations = allocations
            };
            return View(model);
        }

        // GET: LeaveAllocationController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveAllocationController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
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

        // GET: LeaveAllocationController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var leaveallocation = await _unitOfWork.LeaveAllocations.Find(q => q.Id == id,
                includes: new List<string> { "Employee", "LeaveType"});
            var model = _mapper.Map<EditLeaveAllocationVM>(leaveallocation);
            return View(model);
        }

        // POST: LeaveAllocationController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditLeaveAllocationVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
               // var record = await _leaveallocationrepo.FindById(model.Id);
                var record = await _unitOfWork.LeaveAllocations.Find(q => q.Id == model.Id);
                record.NumberOfDays = model.NumberOfDays;
                _unitOfWork.LeaveAllocations.Update(record);
                await _unitOfWork.Save();
                //var isSuccess = await _leaveallocationrepo.Update(record);
                //if (!isSuccess)
                //{
                //    ModelState.AddModelError("", "Error while saving");
                //    return View(model);
                //}
                return RedirectToAction(nameof(Details), new { id = model.EmployeeId });

            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveAllocationController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveAllocationController/Delete/5
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
