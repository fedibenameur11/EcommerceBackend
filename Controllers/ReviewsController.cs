using System.Security.Claims;
using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _svc;
        public ReviewsController(IReviewService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAll(int productId)
        {
            var reviews = await _svc.GetForProductAsync(productId);
            return reviews == null ? NotFound() : Ok(reviews);
        }

        [HttpPost]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<ActionResult<ReviewDto>> Upsert(int productId, [FromBody] CreateReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var review = await _svc.UpsertAsync(productId, userId, dto);
            return review == null ? NotFound() : Ok(review);
        }
    }
}