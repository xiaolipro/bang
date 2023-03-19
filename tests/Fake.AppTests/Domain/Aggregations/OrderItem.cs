﻿using System;
using Fake;
using Fake.Domain.Entities;

namespace Domain.Aggregations;

public class OrderItem:Entity<Guid>
{
    private string _productName;
    private string _pictureUrl;
    private decimal _unitPrice;
    private decimal _discount;
    private int _units;
    
    public int ProductId { get; private set; }
    
    public OrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
    {
        if (units <= 0)
        {
            throw new BusinessException("Invalid number of units");
        }

        if ((unitPrice * units) < discount)
        {
            throw new BusinessException("The total of order item is lower than applied discount");
        }

        ProductId = productId;

        _productName = productName;
        _unitPrice = unitPrice;
        _discount = discount;
        _units = units;
        _pictureUrl = pictureUrl;
    }

    public string GetPictureUri() => _pictureUrl;
    
    public string GetProductName()
    {
        return _productName;
    }
    
    public decimal GetCurrentDiscount()
    {
        return _discount;
    }

    public int GetUnits()
    {
        return _units;
    }

    public decimal GetUnitPrice()
    {
        return _unitPrice;
    }
    
    
    public void SetNewDiscount(decimal discount)
    {
        if (discount < 0)
        {
            throw new BusinessException("Discount is not valid");
        }

        _discount = discount;
    }
    
    public void AddUnits(int units)
    {
        if (units < 0)
        {
            throw new BusinessException("Invalid units");
        }

        _units += units;
    }
}