using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace eShop.Domain
{
    public class Order
    {
        private List<OrderItem> _orderItems;

        public Order()
        {
            _orderItems = new List<OrderItem>();
            OrderNumber = Guid.NewGuid().ToString();
        }

        public string OrderNumber { get; private set; }

        public IReadOnlyCollection<OrderItem> OrderItems
        {
            get => _orderItems.ToImmutableList();
            private set => _orderItems = value.ToList();
        }

        public void AddOrderItem(OrderItem orderItem)
        {
            _orderItems.Add(orderItem);
        }

        public void RemoveOrderItem(OrderItem orderItem)
        {
            _orderItems.Add(orderItem);
        }
    }
}