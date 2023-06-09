﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using GetInItBackEnd.Authentication;
using GetInItBackEnd.Authorization;
using GetInItBackEnd.Entities;
using GetInItBackEnd.Exceptions;
using GetInItBackEnd.Middleware;
using GetInItBackEnd.Models;
using GetInItBackEnd.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GetInItBackEnd.Services.AccountServices;

public class AccountService : IAccountService
{
    private readonly GetInItDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountService> _logger;
    private readonly IPasswordHasher<Account> _passwordHasher;
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly IUserContextService _userService;
    private readonly IAuthorizationService _authorizationService;

    public AccountService(GetInItDbContext dbContext, IMapper mapper, ILogger<AccountService> logger,IPasswordHasher<Account> passwordHasher, AuthenticationSettings authenticationSettings, IUserContextService userService, IAuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _authenticationSettings = authenticationSettings;
        _userService = userService;
        _authorizationService = authorizationService;
    }

    public async Task<AccountDto> GetAccountById(int id)
    {
        var account = await _dbContext.Accounts
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account is null) throw new NotFoundException("Account not found");

        var result = _mapper.Map<AccountDto>(account);
        return result;

      
    }

    public async Task ChangePassword(UpdatePasswordDto passwordDto)
    {
        var account = GetUserFromToken();
        var hashedPassword = _passwordHasher.HashPassword(account.Result, passwordDto.Password);
        account.Result.PasswordHash = hashedPassword;
        await _dbContext.SaveChangesAsync();
    }

    public async Task ChangeEmail(UpdateEmailDto dto)
    {
        var account = GetUserFromToken();
        account.Result.Email = dto.Email;
        await _dbContext.SaveChangesAsync();
    }
    public Task<ProfileDto> GetAccountProfile()
    {
        var account =  _dbContext.Accounts.FirstOrDefault(a => a.Id == _userService.GetUserId);
        var profile = new ProfileDto
        {
            Name = account.Name,
            LastName = account.LastName,
            Email = account.Email,
            Role = account.Role.ToString()
        };
        return Task.FromResult(profile);
    }

    public async Task<IEnumerable<AccountDto>> GetAllAccount()
    {
        var companyId = _userService.GetCompanyId;
        var accounts = await _dbContext.Accounts
            .Include(a => a.Company)
            .Where(a => a.CompanyId == companyId)
            .ToListAsync();
     
        var accountsDto = _mapper.Map<List<AccountDto>>(accounts);
        return accountsDto;
    }

    public async Task<int> RegisterEmployee(CreateEmployeeDto accountDto)
    {
        var account = _mapper.Map<Account>(accountDto);
        account.CreatedById = _userService.GetUserId;
        account.CompanyId = _userService.GetCompanyId;
        var hashedPassword = _passwordHasher.HashPassword(account, accountDto.Password);

        account.PasswordHash = hashedPassword;
        await _dbContext.Accounts.AddAsync(account);
        await _dbContext.SaveChangesAsync();
        return account.Id;
    }

    public async Task DeleteCompany()
    {
        var companyId = _userService.GetCompanyId;

        var company = await _dbContext.Companies
            .Include(c=> c.Accounts)
            .Include(c => c.Address)
            .Include(c => c.Offers)
                .ThenInclude(o => o.JobApplications)
            .FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            throw new NotFoundException("Company Not Found");
        }
        _dbContext.RemoveRange(company);
        await _dbContext.SaveChangesAsync();

    }

    public async Task DeleteAccount(int? id)
    {
        
        var accountId = id ?? _userService.GetUserId.GetValueOrDefault();
        
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId);
        
        if (account == null)
        {
            throw new NotFoundException("User not found.");
        }
        _dbContext.Accounts.Remove(account);
        await _dbContext.SaveChangesAsync();
       
    }
    public async Task<int> RegisterCompanyAccount(CreateAccountDto accountDto)
    {
        var account = _mapper.Map<Account>(accountDto);
        if (accountDto.CreateCompanyDto is not null)
        {
            account.Company = _mapper.Map<Company>(accountDto.CreateCompanyDto);
            account.Company.Address = _mapper.Map<Address>(accountDto.CreateCompanyDto.AddressDto);
        }
        var hashedPassword = _passwordHasher.HashPassword(account, accountDto.Password);

        account.PasswordHash = hashedPassword;
        await _dbContext.Accounts.AddAsync(account);
        await _dbContext.SaveChangesAsync();
        return account.Id;
    }
    public async Task<int> RegisterUser(RegisterUserDto userDto)
    {
        var account = _mapper.Map<Account>(userDto);
    
        var hashedPassword = _passwordHasher.HashPassword(account, userDto.Password);

        account.PasswordHash = hashedPassword;
        await _dbContext.Accounts.AddAsync(account);
        await _dbContext.SaveChangesAsync();
        return account.Id;
    }
    public async Task<string> GenerateJwt(LoginDto dto)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (account is null)
        {
            throw new BadRequestException("Invalid email ");
            
        }
        var result =   _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed )
        {
            throw new BadRequestException("Invalid  password");
        }

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Actor, account.CompanyId.ToString()),
            new Claim(ClaimTypes.Name, $"{account.Name}"),
            new Claim(ClaimTypes.Role, $"{account.Role}"),
            new Claim(ClaimTypes.Surname, $"{account.LastName}"),
            new Claim(ClaimTypes.Email, $"{account.Email}")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationSettings.JwtKey));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(_authenticationSettings.JwtExpireDays);

        var token = new JwtSecurityToken(_authenticationSettings.JwtIssuer, _authenticationSettings.JwtIssuer, claims,
            expires: expires,
            signingCredentials: cred);

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }

    private Task<Account> GetUserFromToken()
    {
        var account =  _dbContext.Accounts.FirstOrDefault(a => a.Id == _userService.GetUserId);
        if (_userService.User != null)
        {
            var authorizationResultUpdate = _authorizationService.AuthorizeAsync(_userService.User, account,
                new ResourceOperationRequirement(ResourceOperation.Update)).Result;
            var authorizationResultCreate = _authorizationService.AuthorizeAsync(_userService.User, account,
                new ResourceOperationRequirement(ResourceOperation.Create)).Result;
            var authorizationResultRead = _authorizationService.AuthorizeAsync(_userService.User, account,
                new ResourceOperationRequirement(ResourceOperation.Read)).Result;
            if (!authorizationResultUpdate.Succeeded || !authorizationResultCreate.Succeeded || !authorizationResultRead.Succeeded)
            {
                throw new ForbidException();
            }
        }
        if (account is null) throw new NotFoundException("Account Not Found");
        return Task.FromResult(account);
    }
    

  
}