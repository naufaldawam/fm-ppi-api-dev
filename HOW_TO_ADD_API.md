# How to Add a New API Resource

Every new resource (Order, Invoice, Category, etc.) follows the same 5-step pattern.
Use `Product` as the template — search & replace "Product" with your resource name.

---

## Example: Add "Order" resource

### Step 1 — Entity (`Domain/Entities/Order.cs`)
```csharp
public class Order : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, etc.
    public string? Notes { get; set; }
}
```

### Step 2 — DTOs (`Application/DTOs/ServiceDTOs.cs`)
```csharp
public class CreateOrderRequest { ... }
public class UpdateOrderRequest { ... }
public class OrderDto { ... }
public class OrderFilterRequest { ... }
```

### Step 3 — Service (`Application/Services/OrderService.cs`)
```csharp
public interface IOrderService
{
    Task<ApiResponse<PagedResponse<OrderDto>>> GetAllAsync(OrderFilterRequest filter);
    Task<ApiResponse<OrderDto>> GetByIdAsync(string id);
    Task<ApiResponse<OrderDto>> CreateAsync(CreateOrderRequest request, string userId);
    Task<ApiResponse<OrderDto>> UpdateAsync(string id, UpdateOrderRequest request, string userId);
    Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
}

public class OrderService : IOrderService { ... }
```

### Step 4 — Register in `API/Program.cs`
```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
```

### Step 5 — Controller (`API/Controllers/OrdersController.cs`)
```csharp
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    [HttpGet]
    [RequirePermission("orders.read")]
    public async Task<IActionResult> GetAll([FromQuery] OrderFilterRequest filter) { ... }

    [HttpPost]
    [RequirePermission("orders.create")]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request) { ... }
    // etc.
}
```

### Step 6 — DbContext (`Infrastructure/Persistence/ServiceDbContext.cs`)
```csharp
// Add DbSet:
public DbSet<Order> Orders { get; set; }

// Add config in OnModelCreating:
modelBuilder.Entity<Order>(entity =>
{
    entity.ToTable("Orders");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
});
```

### Step 7 — Interface (`Application/Interfaces/IServiceDbContext.cs`)
```csharp
DbSet<Order> Orders { get; set; }
```

### Step 8 — Migration
```bash
cd MyService.API
dotnet ef migrations add AddOrders --project ../MyService.Infrastructure
dotnet ef database update --project ../MyService.Infrastructure
```

---

## Access Control Quick Reference

```csharp
[AllowAnonymous]                         // No login needed
[Authorize]                              // Must be logged in (any role)
[Authorize(Roles = "Admin")]             // Must have Admin role
[Authorize(Roles = "Admin,Manager")]     // Admin OR Manager
[RequirePermission("orders.read")]       // Must have exact permission claim in JWT
```

Permissions come from AuthService:
- Create them via `POST /api/permissions`
- Assign to a role via `POST /api/roles/assign-permission`
- Then the JWT issued by AuthService will contain those permission claims

---

## ICurrentUser Quick Reference

```csharp
_currentUser.UserId         // "abc-123"
_currentUser.Email          // "user@example.com"
_currentUser.Roles          // ["Admin", "Manager"]
_currentUser.Permissions    // ["products.read", "orders.create"]
_currentUser.IsAuthenticated // true/false
_currentUser.HasRole("Admin")           // true/false
_currentUser.HasPermission("orders.read") // true/false
```

---

## HTTP Status Codes Quick Reference

```csharp
return Ok(result);           // 200 — success
return BadRequest(result);   // 400 — validation error
return Unauthorized();       // 401 — not logged in
return Forbid();             // 403 — no permission
return NotFound(result);     // 404 — not found
```
