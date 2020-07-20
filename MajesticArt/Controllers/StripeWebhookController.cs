﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MajesticArt.Models;
using MajesticArt.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

namespace MajesticArt.Controllers
{
    [Route("api/[controller]")]
    public class StripeWebhookController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly IOrderService orderService;
        private readonly IProductService productService;
        private readonly UserManager<ApplicationUser> userManager;

        public StripeWebhookController(
            IConfiguration configuration,
            IOrderService orderService,
            IProductService productService,
            UserManager<ApplicationUser> userManager
            )
        {
            this.configuration = configuration;
            this.orderService = orderService;
            this.productService = productService;
            this.userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            string secret = configuration["Stripe:WebhookSecret"];
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = Stripe.EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], secret);

                if (stripeEvent.Type == Stripe.Events.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    await HandleCheckoutSessionCompleted(session);
                }

                return Ok();
            }
            catch (Stripe.StripeException e)
            {
                Console.WriteLine(e.Message);
                return BadRequest();
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            var service = new SessionService();
            var priceService = new Stripe.PriceService();
            var priceOptions = new Stripe.PriceGetOptions { Expand = new List<string> { "product" } };
            Stripe.StripeList<Stripe.LineItem> lineItems = service.ListLineItems(session.Id);
            var productIds = new List<string>();
            var userId = "";

            productIds.AddRange(lineItems.Select(item => {
                var price = priceService.Get(item.PriceId, priceOptions);
                userId = price.Product.Metadata["UserId"];
                return price.Product.Metadata["AppId"];
            }));

            await AddOrder(productIds, userId);
        }

        private async Task AddOrder(List<string> productIds, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            var products = new List<Product>();

            foreach (var id in productIds)
            {
                var product = await productService.Get(int.Parse(id));
                products.Add(product);
            }

            Order order = new Order
            {
                Products = products,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                User = user,
                UserId = user.Id
            };

            await orderService.Add(order);
        }
    }
}
