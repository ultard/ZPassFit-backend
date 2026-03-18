using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class ClientService(IClientRepository clientRepository) : IClientService
{
    public async Task<ClientResponse?> GetMeAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        return client == null ? null : Map(client);
    }

    public async Task<ClientResponse> UpsertMeAsync(string userId, UpsertClientMeRequest request)
    {
        var existing = await clientRepository.GetByUserIdAsync(userId);
        if (existing == null)
        {
            var client = new Client
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LastName = request.LastName,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                BirthDate = request.BirthDate,
                Gender = request.Gender,
                Phone = request.Phone,
                Email = request.Email,
                Notes = request.Notes
            };

            await clientRepository.AddAsync(client);
            return Map(client);
        }

        existing.LastName = request.LastName;
        existing.FirstName = request.FirstName;
        existing.MiddleName = request.MiddleName;
        existing.BirthDate = request.BirthDate;
        existing.Gender = request.Gender;
        existing.Phone = request.Phone;
        existing.Email = request.Email;
        existing.Notes = request.Notes;

        await clientRepository.UpdateAsync(existing);
        return Map(existing);
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid id)
    {
        var client = await clientRepository.GetByIdAsync(id);
        return client == null ? null : Map(client);
    }

    private static ClientResponse Map(Client c) =>
        new(
            c.Id,
            c.LastName,
            c.FirstName,
            c.MiddleName,
            c.BirthDate,
            c.Gender,
            c.Phone,
            c.Email,
            c.RegistrationDate,
            c.Status,
            c.Bonuses,
            c.Notes
        );
}