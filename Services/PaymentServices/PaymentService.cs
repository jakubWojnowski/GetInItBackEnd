﻿using System.Collections.Generic;
using Azure;
using GetInItBackEnd.Entities;
using GetInItBackEnd.Models.PaymentsDtos;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;
using Stripe.Checkout;


namespace GetInItBackEnd.Services.PaymentServices;

public class PaymentService : IPaymentService
{
    private readonly GetInItDbContext _dbContext;
    private readonly ILogger _logger;

    public PaymentService(GetInItDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Session> MakePayment()
    {
        var payment =  _dbContext.Payments;
        var domain = "http://localhost:3000";
        var options = new SessionCreateOptions
        {
            
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    // Provide the exact Price ID (for example, pr_1234) of the product you want to sell
                    Price = "price_1NG1yHLpkQnyrIfC1TdGrsY3",
                    Quantity = 1,
                }
            },
            Mode = "payment",
            SuccessUrl = domain + "/paymentConfirmed",
            CancelUrl = domain + "/paymentRefused",
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
        };
        var service = new SessionService();
        Session session = await service.CreateAsync(options);
        return session;


    }

    public async Task<int> PaymentToDatabase(HttpRequest request)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);

            if (stripeEvent.Type == Events.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                var paymentDto = new CreatePaymentDto
                {
                    PaymentDate = DateTime.Now,
                    Amount = paymentIntent.Amount / 100M,
                    StripePaymentId = paymentIntent.Id,
                    PaymentStatus = "Succeeded"
                };

                var payment = new Payment
                {
                    PaymentDate = paymentDto.PaymentDate,
                    Amount = paymentDto.Amount,
                    StripePaymentId = paymentDto.StripePaymentId,
                    PaymentStatus = paymentDto.PaymentStatus
                };

                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();

                return 1; // W przypadku sukcesu, zwróć 1
            }

            return 0; // W przypadku niepowodzenia, zwróć 0
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return -1; // W przypadku błędu, zwróć -1
        }
    }

}
