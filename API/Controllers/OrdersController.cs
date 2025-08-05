using API.DTOs;
using API.Extensions;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class OrdersController(ICartService cartService, IUnitOfWork unit) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
    {
        string email = User.GetEmail();
        ShoppingCart? cart = await cartService.GetCartAsync(orderDto.CartId);

        if (cart == null) return BadRequest("Cart not found");
        if (cart.PaymentIntentId == null) return BadRequest("No payment intent for this order");

        List<OrderItem> items = [];
        foreach (CartItem item in cart.Items)
        {
            Product? productItem = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
            if (productItem == null) return BadRequest("Problem with the order");

            ProductItemOrdered itemOrdered = new()
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                PictureUrl = item.PictureUrl
            };

            OrderItem orderItem = new()
            {
                ItemOrdered = itemOrdered,
                Price = item.Price,
                Quantity = item.Quantity
            };

            items.Add(orderItem);
        }

        DeliveryMethod? deliveryMethod = await unit.Repository<DeliveryMethod>()
            .GetByIdAsync(orderDto.DeliveryMethodId);
        if (deliveryMethod == null) return BadRequest("No delivery method selected");

        Order order = new()
        {
            OrderItems = items,
            DeliveryMethod = deliveryMethod,
            ShippingAddress = orderDto.ShippingAddress,
            SubTotal = items.Sum(x => x.Price * x.Quantity),
            PaymentSummary = orderDto.PaymentSummary,
            PaymentIntentId = cart.PaymentIntentId,
            BuyerEmail = email,
            Discount = orderDto.Discount
        };

        unit.Repository<Order>().Add(order);
        if (await unit.Complete()) return order;

        return BadRequest("Problem created order");
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser()
    {
        OrderSpecification spec = new(User.GetEmail());
        IReadOnlyList<Order> orders = await unit.Repository<Order>().ListAsync(spec);
        List<OrderDto> OrdersToReturn = [.. orders.Select(o => o.ToDto())];
        return Ok(OrdersToReturn);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        OrderSpecification spec = new(User.GetEmail(), id);
        Order? order = await unit.Repository<Order>().GetEntityWithSpec(spec);
        if (order == null) return NotFound();
        return order.ToDto();
    }
}
