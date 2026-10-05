using Microsoft.AspNetCore.Mvc;
using WebApplication3.Application.Orders.CreateOrder;
using WebApplication3.Web.Extensions;
using WebApplication3.Web.Models;

namespace WebApplication3.Web.Controllers
{




    public class OrdersController : Controller
    {
        private readonly CreateOrderUseCase _createOrder;

        public OrdersController(CreateOrderUseCase createOrder)
        {
            _createOrder = createOrder;
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateOrderViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // ViewModel → Command
            var command = new CreateOrderCommand(
                vm.CustomerEmail,
                vm.Items.Select(i => new CreateOrderItem(i.ProductId, i.Quantity)).ToList());

            // Use Case
            var result = await _createOrder.ExecuteAsync(command, ct);

            // Result → IActionResult
            return result.ToActionResult(this, id => RedirectToAction(nameof(Details), new { id }));
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}
