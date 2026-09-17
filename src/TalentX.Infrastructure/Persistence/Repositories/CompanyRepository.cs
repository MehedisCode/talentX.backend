using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _context.Companies.AddAsync(company, cancellationToken);
    }

    public void Update(Company company)
    {
        _context.Companies.Update(company);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.EmployerId == employerId, cancellationToken);
    }

    public async Task DeleteByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.EmployerId == employerId, cancellationToken);

        if (company is not null)
        {
            _context.Companies.Remove(company);
        }
    }
}
