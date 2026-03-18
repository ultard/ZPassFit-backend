using ZPassFit.Data.Models;

namespace ZPassFit.Data.Repositories.Employees;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int id);
}