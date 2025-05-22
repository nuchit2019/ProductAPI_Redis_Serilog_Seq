
# สร้าง ProductAPI ด้วย .NET 8 พร้อม Redis Cache, Serilog Logging และระบบ Centralized Log Monitoring ด้วย Seq


.NET 8 CRUD Product API (Dapper, MSSQL, Redis, Serilog, Seq, Clean Architecture)

## Features
- Clean Architecture, SOLID
- Dapper ORM, Local MSSQL
- Redis Cache (Enquiry)
- Global Exception Handling (Middleware)
- Response Wrapper (ApiResponse)
- Serilog Logging (Console/File/Seq)
- Docker Compose: Redis + Seq

## Quick Start

1. **Clone & Restore**
    ```sh
    git clone ...
    cd ProductAPI
    dotnet restore
    ```

2. **Start Redis + Seq**
    ```sh
    docker-compose up -d
    ```

3. **Create Product Table in MSSQL**
    ```sql
   CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Stock INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL);
    ```

4. **Edit ConnectionString in `appsettings.json`**
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductDb;Trusted_Connection=True;"
    }
    ```

5. **Run**
    ```sh
    dotnet run --project Api
    ```

6. **Test API**
    - `GET    /api/product`
    - `GET    /api/product/{id}`
    - `POST   /api/product`
    - `PUT    /api/product/{id}`
    - `DELETE /api/product/{id}`

7. **Log Monitoring**
    - [http://localhost:5341](http://localhost:5341) (Seq Dashboard)

## Project Structure
   ```
ProductAPIRedisCache/
├── Api/                   # ASP.NET Core WebAPI (Controllers, Program.cs)
├── Application/           # Application Layer (Services, Interfaces)
├── Domain/                # Domain Entities, Interfaces
├── Infrastructure/        # Repository, Database, Redis, Cache
├── Common/                # Utilities (ApiResponse, Middleware)
├── Middleware/
├── README.md              # คู่มือฉบับนี้
└── docker-compose.yml
   ```
## API Response Format

```json
{
  "success": true,
  "message": "รายละเอียดผลลัพธ์",
  "data": {...}
}
````

## Global Exception

* ทุก error จะ log ลง Serilog (Console, File, Seq)
* Client จะได้รับ message กลางๆ, ไม่ส่งรายละเอียด code (security best practice) 

 
 

