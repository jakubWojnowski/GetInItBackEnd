﻿using AutoMapper;
using GetInItBackEnd.Entities;
using GetInItBackEnd.Exceptions;
using GetInItBackEnd.Models.Account;
using GetInItBackEnd.Models.Company;
using Microsoft.EntityFrameworkCore;

namespace GetInItBackEnd.Services.CompanyServices;

public class CompanyService : ICompanyService
{
    private readonly GetInItDbContext _dbContext;
    private readonly IMapper _mapper;

    public CompanyService(GetInItDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<int> CreateAccount(int companyId, CreateAccountDto accountDto)
    {
        await GetCompanyId(companyId);
        var accountEntity = _mapper.Map<Account>(accountDto);
        await _dbContext.Accounts.AddAsync(accountEntity);
        await _dbContext.SaveChangesAsync();

        return accountEntity.Id;
    }

    public async Task<List<CompanyDto>> GetAllCompanies()
    {
        var companies = await _dbContext.Companies
            .Include(z => z.Accounts)
            //.Include(z => z.Address)
            .ToListAsync();
        var companyDto = _mapper.Map<List<CompanyDto>>(companies);
        return companyDto;
    }

    private async Task<Company?> GetCompanyId(int companyId)
    {
        var company = _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null) throw new NotFoundException("Company Not Found");

        return await company;

    }
    
}