# Database Smart Parking

Tài liệu này hướng dẫn dựng PostgreSQL Development, cấu hình Connection String, chạy EF Core Migration và kiểm tra schema `parking` trong cấu trúc Monorepo.

Task hiện tại chỉ tạo nền tảng Database và 3 bảng Parking. Chưa chuyển API JSON/mock sang PostgreSQL.

## 1. Công nghệ đang dùng

- PostgreSQL 17 (Docker Compose)
- Entity Framework Core 10.0.12
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
- Công cụ CLI: dotnet-ef 10.0.13 (local tool; bản 10.0.12 restore được nhưng SDK 10 không chạy lệnh `dotnet ef`)
- Database Development: `smartparking_dev`
- Schema hiện tại: `parking`
- Schema định hướng sau này: `identity`, `booking`, `payment`, `iot`, `recognition`

ID các bảng Parking dùng số nguyên (`integer identity`) để không phá API hiện tại.

## 2. Yêu cầu môi trường

- .NET SDK 10
- Docker Desktop / Docker Engine + Docker Compose v2
- Công cụ EF: chạy `dotnet tool restore` ở thư mục gốc repository
- Build Solution: `dotnet restore backend/Prj.CarParking.slnx` và `dotnet build backend/Prj.CarParking.slnx`

Máy chưa có Docker thì vẫn clone code, restore, build được. Chưa chạy được container và chưa apply Migration lên PostgreSQL.

## 3. Cách chạy Docker Compose

Từ thư mục gốc `smartparking`:

```powershell
copy .env.example .env
```

Sửa `POSTGRES_PASSWORD` trong file `.env`. Không commit file `.env`.

```powershell
docker compose up -d
docker compose ps
```

PostgreSQL chỉ bind `127.0.0.1`. Đổi cổng host bằng `POSTGRES_HOST_PORT` trong `.env` nếu máy đã dùng 5432.

Volume dữ liệu: `smartparking_pgdata`. Không xóa volume này trừ khi được nhóm thống nhất.

## 4. Cách cấu hình Connection String

Không đưa mật khẩu vào Git.

- **Cho WebAPI runtime (`dotnet run`)**: Khuyến nghị dùng User Secrets (được nạp tự động vào `IConfiguration`):
  ```powershell
  dotnet user-secrets set "ConnectionStrings:Parking" "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking;Password=YOUR_PASSWORD" --project backend/src/Parking.WebAPI
  ```
- **Cho EF Core CLI (`dotnet ef database update`)**: `ParkingDbContextFactory` chạy ở design-time và đọc biến môi trường `ConnectionStrings__Parking` (không đọc User Secrets). Cần đặt biến môi trường trước khi chạy lệnh CLI:
  ```powershell
  $env:ConnectionStrings__Parking = "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking;Password=YOUR_PASSWORD"
  ```

Không để password hoặc Connection String đầy đủ trong Git. File `backend/src/Parking.WebAPI/appsettings.json` giữ `ConnectionStrings:Parking` rỗng. `appsettings.Development.json` không chứa Connection String.

Khi chưa set User Secrets / biến môi trường, WebAPI vẫn chạy API JSON/mock và ghi log hướng dẫn. Ứng dụng không kết nối PostgreSQL lúc khởi động vì repository hiện tại chưa dùng DB.

## 5. Cách chạy EF Core Migration

Cài tool (một lần trên mỗi máy tại thư mục gốc repository):

```powershell
dotnet tool restore
```

Kiểm tra danh sách Migration và Model:

```powershell
dotnet ef migrations list --project backend/src/Parking.Persistence --startup-project backend/src/Parking.WebAPI
dotnet ef migrations has-pending-model-changes --project backend/src/Parking.Persistence --startup-project backend/src/Parking.WebAPI
```

Tạo migration mới (chỉ khi đổi Entity/Configuration):

```powershell
dotnet ef migrations add TenMigration --project backend/src/Parking.Persistence --startup-project backend/src/Parking.WebAPI --output-dir Migrations
```

Áp dụng migration lên Development:

```powershell
$env:ConnectionStrings__Parking = "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking;Password=YOUR_PASSWORD"
dotnet ef database update --project backend/src/Parking.Persistence --startup-project backend/src/Parking.WebAPI
```

Không dùng `EnsureCreated()`. Không tự chạy Migration trên Production.

## 6. Cách kiểm tra bảng

```powershell
docker compose exec postgres psql -U smartparking -d smartparking_dev -c "\dn"
docker compose exec postgres psql -U smartparking -d smartparking_dev -c "\dt parking.*"
docker compose exec postgres psql -U smartparking -d smartparking_dev -c "\d parking.parking_lots"
docker compose exec postgres psql -U smartparking -d smartparking_dev -c "\d parking.parking_floors"
docker compose exec postgres psql -U smartparking -d smartparking_dev -c "\d parking.parking_slots"
```

Migration đầu tiên tạo schema `parking` và 3 bảng. Không seed bãi/tầng/slot giả.

## 7. Cách chạy WebAPI

```powershell
dotnet run --project backend/src/Parking.WebAPI --launch-profile http
```

Kiểm tra API cũ:

- `GET http://localhost:5174/parking-lots`
- `GET http://localhost:5174/api/ParkingSlots`

Hai API này vẫn đọc JSON/mock, chưa đọc PostgreSQL.

## 8. Cách dừng container an toàn

```powershell
docker compose stop
```

Dừng và xóa container nhưng giữ dữ liệu:

```powershell
docker compose down
```

Không chạy `docker compose down -v` vì lệnh đó xóa volume `smartparking_pgdata`.

## 9. Backup / restore Development Database

Backup:

```powershell
docker compose exec postgres pg_dump -U smartparking -d smartparking_dev --format=custom --file=/tmp/smartparking_dev.dump
docker compose cp postgres:/tmp/smartparking_dev.dump .\smartparking_dev.dump
```

Restore vào database Development hiện tại:

```powershell
docker compose cp .\smartparking_dev.dump postgres:/tmp/smartparking_dev.dump
docker compose exec postgres pg_restore -U smartparking -d smartparking_dev --clean --if-exists /tmp/smartparking_dev.dump
```

Chỉ restore lên máy local. Không restore lên Production hoặc Shared Development trừ khi nhóm thống nhất.

## 10. Lỗi thường gặp

| Lỗi | Cách xử lý |
|---|---|
| `docker` không nhận lệnh | Cài Docker Desktop, mở Docker, mở lại terminal |
| Compose báo thiếu `POSTGRES_PASSWORD` | Copy `.env.example` thành `.env` và điền password |
| Cổng 5432 đã bị chiếm | Đổi `POSTGRES_HOST_PORT` trong `.env`, nhớ đổi Port trong Connection String |
| `dotnet ef` không chạy | Chạy `dotnet tool restore` ở thư mục gốc |
| `password authentication failed` | Kiểm tra User Secrets / biến môi trường, không copy password từ Git |
| `relation already exists` | Database đã có schema. Không xóa volume. Kiểm tra lịch sử Migration trước khi tạo mới |
| API JSON vẫn chạy nhưng DB chưa có | Đúng với task hiện tại. API chưa đọc PostgreSQL |

## 11. Thành viên mới dựng Database

1. Clone repository, checkout branch làm việc.
2. Cài .NET SDK 10 và Docker Desktop.
3. `dotnet tool restore`
4. Copy `.env.example` thành `.env`, đặt password riêng.
5. `docker compose up -d`
6. Cấu hình Connection String:
   - Cho WebAPI runtime: `dotnet user-secrets set "ConnectionStrings:Parking" "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking;Password=YOUR_PASSWORD" --project backend/src/Parking.WebAPI`
   - Cho EF Core CLI (`ParkingDbContextFactory` đọc biến môi trường): `$env:ConnectionStrings__Parking = "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking;Password=YOUR_PASSWORD"`
7. `dotnet ef database update --project backend/src/Parking.Persistence --startup-project backend/src/Parking.WebAPI`
8. Kiểm tra schema/bảng bằng `psql`.
9. `dotnet run --project backend/src/Parking.WebAPI --launch-profile http` và gọi 2 API cũ.

Không tạo bãi/tầng/slot giả. Dữ liệu thật chờ task tiếp theo.

## 12. Loại phương tiện (`vehicle_type`)

Quy ước hiện tại khớp Frontend Song. Không thêm mã khác trong task này.

| Mã | Ý nghĩa |
|---|---|
| `car` | Ô tô |
| `moto` | Xe máy |
| `ev` | Xe điện |

Đây là quy ước tương thích hiện tại. Việc tách vehicle category (ô tô/xe máy) và propulsion type (xăng/điện) sẽ xem xét khi có yêu cầu nghiệp vụ.

Trạng thái chỗ đỗ (`status`): `available`, `occupied`, `reserved`, `out_of_service`. API cũ vẫn dùng `IsOccupied` (bool) và chưa đọc cột này.

## 13. Nợ kỹ thuật cho TASK 03C

Domain Entity đang kiêm API JSON và bảng PostgreSQL:

- `[JsonIgnore]` trên field DB để không đổi JSON response hiện tại.
- EF `Ignore` các field API cũ (`AvailableSlots`, `HourlyRate`, `SlotNumber`, `Floor`, `IsOccupied`).
- ID vẫn là `int`, không đổi sang UUID.

TASK 03C nên tách DTO cho API, bỏ phụ thuộc `System.Text.Json` khỏi Domain. Chưa chuyển Repository JSON/mock sang PostgreSQL trong task này.

## 14. Quy định quản lý Migration trên Git

- Mỗi thay đổi schema phải có Migration mới, commit cùng Entity/Configuration.
- Không sửa file Migration đã apply trên máy khác.
- Không xóa `__EFMigrationsHistory`.
- Không chạy Migration phá hủy dữ liệu (`DropTable`, xóa volume) nếu chưa có thống nhất của nhóm.
- Tránh hai người tạo Migration song song trên cùng baseline. Pull mới nhất trước khi `migrations add`.
- Production/Shared Development do Trí kiểm soát. Không tự apply lên môi trường chung.
- Song không truy cập trực tiếp database; Frontend chỉ gọi REST API.
