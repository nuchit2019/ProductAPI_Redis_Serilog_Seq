 
# ProductAPI with Redis Cache, Serilog Logging, and Centralized Monitoring with Seq


**CRUD Product API (.NET 8, Dapper, MSSQL, Redis,Seq, Clean Architecture)**

---

## **คุณสมบัติเด่น**

* โครงสร้าง Clean Architecture (แบ่งชั้นชัดเจน)
* Dapper ORM (เร็ว, เข้าใจง่าย)
* รองรับ Redis Cache (StackExchange.Redis)
* MSSQL LocalDB ใช้งานง่าย
* Global Exception Middleware (จับ error/log อัตโนมัติ)
* Response Wrapper (ApiResponse<T>)
* Serilog Logging (พร้อมต่อกับ Seq, File, Console ฯลฯ)
* ใช้ Docker Compose สำหรับ Redis, Seq
* DI เต็มรูปแบบ (Testable, ขยายง่าย)

---

## **โครงสร้างไฟล์**

```
ProductAPI/
├── Api/                   # ASP.NET Core WebAPI (Controllers, Program.cs)
├── Application/           # Application Layer (Services, Interfaces)
├── Domain/                # Domain Entities, Interfaces
├── Infrastructure/        # Repository, Database, Redis, Cache
├── Common/                # Utilities (ApiResponse, Middleware)
├── README.md              # คู่มือฉบับนี้
└── docker-compose.yml
```

---

## **คู่มือเริ่มต้น**

### **1. ติดตั้ง Package ที่ต้องใช้**

```sh
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package StackExchange.Redis
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

---

### **2. สร้าง Table MSSQL สำหรับ Product**

```sql
CREATE DATABASE ProductDb;

CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL
);
```

---

### **3. Docker Compose สำหรับ Redis + Seq**

**docker-compose.yml**

```yaml
version: '3.9'
services:
  redis:
    image: "redis:7.2"
    container_name: "my-redis"
    ports:
      - "6379:6379"
    restart: always

  seq:
    image: datalust/seq
    container_name: seq
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:80"
    volumes:
      - seqdata:/data
    restart: always

volumes:
  seqdata:
```

**รัน**

```sh
docker-compose up -d
```

* Redis: : [http://localhost:6379](http://localhost:6379)
* Seq (ดู log): [http://localhost:5341](http://localhost:5341)

---

### **4. ตั้งค่า Connection String ใน `appsettings.json`**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductDb;Trusted_Connection=True;"
}
```

---

### **5. ตัวอย่างการ Register DI และ Serilog ใน `Program.cs`**

```csharp
using Serilog;
using StackExchange.Redis;
using ProductAPI.Common;
using ProductAPI.Infrastructure.Cache;
using ProductAPI.Infrastructure.Database;
using ProductAPI.Infrastructure.Repositories;
using ProductAPI.Application.Services;
using ProductAPI.Application.Interfaces;
using ProductAPI.Domain.Interfaces;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

var app = builder.Build();

// Global Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();
app.Run();
```

---

### **6. ตัวอย่างโค้ด ApiResponse (Record) และ ExceptionMiddleware**

**Common/ApiResponse.cs**

```csharp
namespace ProductAPI.Common;

public record ApiResponse<T>(bool Success, string? Message, T? Data)
{
    public static ApiResponse<T> Ok(T data, string? message = null)
        => new(true, message, data);

    public static ApiResponse<T> Fail(string? message = null)
        => new(false, message, default);
}
```

**Common/ExceptionMiddleware.cs**

```csharp
using System.Net;
using System.Text.Json;
using ProductAPI.Common;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                ArgumentNullException or ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var message = context.Response.StatusCode == 404
                ? "Resource not found."
                : "Internal server error. Please contact support.";

            var response = ApiResponse<string>.Fail(message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
```

---

### **7. ตัวอย่าง Controller**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController(IProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<Product>>.Ok(products));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await service.GetByIdAsync(id);
        if (product is null)
            return NotFound(ApiResponse<Product>.Fail("Product not found."));
        return Ok(ApiResponse<Product>.Ok(product));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var id = await service.CreateAsync(product);
        return CreatedAtAction(nameof(Get), new { id }, ApiResponse<int>.Ok(id, "Product created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        product.Id = id;
        var result = await service.UpdateAsync(product);
        if (!result)
            return NotFound(ApiResponse<Product>.Fail("Product not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Product updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteAsync(id);
        if (!result)
            return NotFound(ApiResponse<Product>.Fail("Product not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Product deleted."));
    }
}
```

---

### **8. API Response Format**

```json
{
  "success": true,
  "message": "Product created.",
  "data": 123
}
```

หรือเมื่อ error:

```json
{
  "success": false,
  "message": "Internal server error. Please contact support.",
  "data": null
}
```

---

### **9. ดู log ได้ที่ Seq**

* เปิด [http://localhost:5341](http://localhost:5341)
* ใช้ search, filter, dashboard ได้แบบ real-time

---

### **10. ดูข้อมูลใน Redis (ดูค่า key/value, ตรวจสอบ cache) 
#### โดยใช้ **Redis GUI Tool**

โปรแกรมฟรีที่ใช้ดู Redis GUI:

* **[RedisInsight](https://redis.com/redis-enterprise/redis-insight/)**

แค่ดาวน์โหลด > Connect ไปที่ Redis server (`localhost:6379`) > จะเห็น key/value ได้ทันที
**ข้อดี:** เหมาะกับ dev, ดู key, แก้ไข, ลบ, inspect ข้อมูลแบบไม่ต้องพิมพ์ command เอง
 ![image](https://github.com/user-attachments/assets/27b8bb42-5ae3-409c-875a-b0fda535802a)


---

### **สรุป**

* ดูผ่าน CLI (docker exec -it ... redis-cli → KEYS \* → GET ...)
* หรือ ใช้ Redis GUI Tools (RedisInsight, FastoRedis ฯลฯ)
* หรือ เขียน Code test (C# / Python ฯลฯ)

---

ถ้าติดตั้ง RedisInsight แล้วมีปัญหา หรืออยากได้ step-by-step พร้อมภาพ แจ้งได้เลยครับ!
**ถ้าอยากดู key ทุกรายการ หรือเจาะค่าบาง key—ระบุ key ที่ต้องการมาด้วยก็ได้ครับ**


---


### **10. คำแนะนำสำหรับมือใหม่**

* เริ่มจาก clone/project นี้
* ต่อยอด entity ใหม่ได้ง่าย (แค่เพิ่ม Model/Repository/Service)
* ศึกษา log จาก Seq, Console, หรือเพิ่ม Sinks อื่นได้
* เพิ่ม cache ด้วย Redis ง่ายมาก (IRedisCacheService) 

---
 
