/*
 * Copyright 2025 ClrSlate Tech labs Private Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

﻿// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace ClrSlate.Agents.TestAgent.Plugins;

/// <summary>
/// A simple invoice plugin that returns mock data.
/// </summary>
public class Product
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; } // Price per unit  

    public Product(string name, int quantity, decimal price)
    {
        Name = name;
        Quantity = quantity;
        Price = price;
    }

    public decimal TotalPrice()
    {
        return Quantity * Price; // Total price for this product  
    }
}

public class Invoice
{
    public string TransactionId { get; set; }
    public string InvoiceId { get; set; }
    public string CompanyName { get; set; }
    public DateTime InvoiceDate { get; set; }
    public List<Product> Products { get; set; } // List of products  

    public Invoice(string transactionId, string invoiceId, string companyName, DateTime invoiceDate, List<Product> products)
    {
        TransactionId = transactionId;
        InvoiceId = invoiceId;
        CompanyName = companyName;
        InvoiceDate = invoiceDate;
        Products = products;
    }

    public decimal TotalInvoicePrice()
    {
        return Products.Sum(product => product.TotalPrice()); // Total price of all products in the invoice  
    }
}

public class InvoiceQueryPlugin
{
    private readonly List<Invoice> _invoices;
    private static readonly Random s_random = new();

    public InvoiceQueryPlugin()
    {
        // Extended mock data with quantities and prices  
        _invoices =
        [
            new("TICKET-XYZ987", "INV789", "Contoso", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 150, 10.00m),
                new("Hats", 200, 15.00m),
                new("Glasses", 300, 5.00m)
            }),
            new("TICKET-XYZ111", "INV111", "XStore", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 2500, 12.00m),
                new("Hats", 1500, 8.00m),
                new("Glasses", 200, 20.00m)
            }),
            new("TICKET-XYZ222", "INV222",  "Cymbal Direct", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 1200, 14.00m),
                new("Hats", 800, 7.00m),
                new("Glasses", 500, 25.00m)
            }),
            new("TICKET-XYZ333", "INV333", "Contoso", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 400, 11.00m),
                new("Hats", 600, 15.00m),
                new("Glasses", 700, 5.00m)
            }),
            new("TICKET-XYZ444", "INV444", "XStore", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 800, 10.00m),
                new("Hats", 500, 18.00m),
                new("Glasses", 300, 22.00m)
            }),
            new("TICKET-XYZ555", "INV555", "Cymbal Direct", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 1100, 9.00m),
                new("Hats", 900, 12.00m),
                new("Glasses", 1200, 15.00m)
            }),
            new("TICKET-XYZ666", "INV666", "Contoso", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 2500, 8.00m),
                new("Hats", 1200, 10.00m),
                new("Glasses", 1000, 6.00m)
            }),
            new("TICKET-XYZ777", "INV777", "XStore", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 1900, 13.00m),
                new("Hats", 1300, 16.00m),
                new("Glasses", 800, 19.00m)
            }),
            new("TICKET-XYZ888", "INV888", "Cymbal Direct", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 2200, 11.00m),
                new("Hats", 1700, 8.50m),
                new("Glasses", 600, 21.00m)
            }),
            new("TICKET-XYZ999", "INV999", "Contoso", GetRandomDateWithinLastTwoMonths(), new List<Product>
            {
                new("T-Shirts", 1400, 10.50m),
                new("Hats", 1100, 9.00m),
                new("Glasses", 950, 12.00m)
            })
        ];
    }

    public static DateTime GetRandomDateWithinLastTwoMonths()
    {
        // Get the current date and time  
        DateTime endDate = DateTime.Now;

        // Calculate the start date, which is two months before the current date  
        DateTime startDate = endDate.AddMonths(-2);

        // Generate a random number of days between 0 and the total number of days in the range  
        var totalDays = (endDate - startDate).Days;
        var randomDays = s_random.Next(0, totalDays + 1); // +1 to include the end date  

        // Return the random date  
        return startDate.AddDays(randomDays);
    }

    [KernelFunction]
    [Description("Retrieves invoices for the specified company and optionally within the specified time range")]
    public IEnumerable<Invoice> QueryInvoices(string companyName, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _invoices.Where(i => i.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase));

        if (startDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= endDate.Value);
        }

        return query.ToList();
    }

    [KernelFunction]
    [Description("Retrieves invoice using the transaction id")]
    public IEnumerable<Invoice> QueryByTransactionId(string transactionId)
    {
        var query = _invoices.Where(i => i.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    [KernelFunction]
    [Description("Retrieves invoice using the invoice id")]
    public IEnumerable<Invoice> QueryByInvoiceId(string invoiceId)
    {
        var query = _invoices.Where(i => i.InvoiceId.Equals(invoiceId, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }
}
