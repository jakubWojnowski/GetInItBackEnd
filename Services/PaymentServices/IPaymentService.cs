﻿using GetInItBackEnd.Models.PaymentsDtos;
using Stripe.Checkout;

namespace GetInItBackEnd.Services.PaymentServices;

public interface IPaymentService
{
    Task<Session> MakePayment();
    public Task<int> PaymentToDatabase(HttpRequest request);
    public Task<int> CreatePayment(CreatePaymentDto dto);
}