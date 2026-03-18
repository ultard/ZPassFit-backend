using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models;

namespace ZPassFit.Data.Repositories.Employees;

public class EmployeeRepository(ApplicationDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(Employee employee)
    {
        await context.Employees.AddAsync(employee);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Employee employee)
    {
        context.Employees.Update(employee);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await GetByIdAsync(id);
        if (employee == null) return;

        context.Employees.Remove(employee);
        await context.SaveChangesAsync();
    }
}