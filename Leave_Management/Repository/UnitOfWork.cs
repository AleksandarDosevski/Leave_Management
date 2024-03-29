﻿using Leave_Management.Contracts;
using Leave_Management.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;
        private IGenericRepository<LeaveType> _leaveTypes;
        private IGenericRepository<LeaveRequest> _leaveRequests;
        private IGenericRepository<LeaveAllocation> _leaveAllocations;
       
        public UnitOfWork(DataContext context)
        {
            _context = context;
        }

        public IGenericRepository<LeaveType> LeaveTypes 
            => _leaveTypes ??= new GenericRepository<LeaveType>(_context);
        public IGenericRepository<LeaveRequest> LeaveRequests
            => _leaveRequests ??= new GenericRepository<LeaveRequest>(_context);

        public IGenericRepository<LeaveAllocation> LeaveAllocations
            => _leaveAllocations ??= new GenericRepository<LeaveAllocation>(_context);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool dispose)
        {
            if(dispose)
            {
                _context.Dispose();
            }
        }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }
    }
}
