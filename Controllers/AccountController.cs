﻿using GetInItBackEnd.Models;
using GetInItBackEnd.Models.Account;
using GetInItBackEnd.Models.Company;
using GetInItBackEnd.Services.AccountServices;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace GetInItBackEnd.Controllers;

[Route("api/account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }
    
    [HttpPost("RegisterAccountCompany")]
    public async Task<ActionResult> RegisterCompanyAccount([FromBody] CreateAccountDto accountDto)
    {
        var id = await _accountService.RegisterAccount(accountDto, null);
       
        return Created($"/api/account/{id}", null);
    }
    [HttpGet("GetAllCompanyAccounts")]
    public async Task<OkObjectResult> GetAll()
    {
        var accountDtos = await _accountService.GetAllAccount();
        return Ok(accountDtos);
    }
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _accountService.GenerateJwt(dto);
        return Ok(token);
    }

    [HttpGet("AccountProfile")]
    public async Task<ActionResult> GetProfileInfo()
    {
        var accountInfo = await _accountService.GetAccountProfile();
        return Ok(accountInfo);
    }

 


}