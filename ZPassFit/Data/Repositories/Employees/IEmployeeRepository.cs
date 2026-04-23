using ZPassFit.Data.Models;

namespace ZPassFit.Data.Repositories.Employees;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Guid id);
}