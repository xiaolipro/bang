﻿using System;
using System.Threading.Tasks;
using Domain.Aggregates.BuyerAggregate;
using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Repositories;

public class AppTestDataBuilder
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Buyer> _buyerRepository;

    public static Guid UserId = new("1fcf46b2-28c3-48d0-8bac-fa53268a2775");
    public static Guid OrderId = new("4d734a0e-3e6b-4bad-bb43-ef8cf1b09633");


    public AppTestDataBuilder(IRepository<Order> orderRepository, IRepository<Buyer> buyerRepository)
    {
        _orderRepository = orderRepository;
        _buyerRepository = buyerRepository;
    }

    public async Task BuildAsync()
    {
        await AddOrder();
        //await AddBuyer();
    }

    private async Task AddBuyer()
    {
        var buyer = new Buyer(UserId, "fake");
        buyer.AddPaymentMethod(CardType.Visa, "fakeAlias", "fakeCardNumber",
            "fakeSecurityNumber", "fakeCardHolderName",
            DateTime.Now.AddYears(1), OrderId);
        await _buyerRepository.AddAsync(buyer);
    }
    
    private async Task AddOrder()
    {
        var street = "fakeStreet";
        var city = "FakeCity";
        var state = "fakeState";
        var country = "fakeCountry";
        var zipcode = "FakeZipCode";
        var cardType = CardType.Amex;
        var cardNumber = "12";
        var cardSecurityNumber = "123";
        var cardHolderName = "FakeName";
        var cardExpiration = DateTime.Now.AddYears(1);
        var address = new Address(street, city, state, country, zipcode);
        var order = new Order(UserId, "fakeName", address,
            cardType, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration);

        order.AddOrderItem(1, "橘子", 9.6m, 9, null);
        order.AddOrderItem(2, "菠萝", 3.6m, 8, null, 3);
        order.SetId(OrderId);
        order.SetPaidStatus();
        await _orderRepository.AddAsync(order);
    }
}