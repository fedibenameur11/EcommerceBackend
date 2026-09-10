using System.ComponentModel.DataAnnotations;
using EcommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    public class UpdateOrderStatusDto
    {
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; }
    }
}
