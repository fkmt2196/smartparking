# ERD schema parking

Migration đầu tiên chỉ tạo schema `parking`. Các schema khác là định hướng, chưa tạo bảng.

## Schema hiện tại

```mermaid
erDiagram
    PARKING_LOTS ||--o{ PARKING_FLOORS : "có nhiều tầng"
    PARKING_FLOORS ||--o{ PARKING_SLOTS : "có nhiều chỗ đỗ"

    PARKING_LOTS {
        int id PK
        varchar code UK
        varchar name
        varchar address
        numeric latitude "nullable"
        numeric longitude "nullable"
        boolean is_active
        timestamptz created_at_utc
        timestamptz updated_at_utc
    }

    PARKING_FLOORS {
        int id PK
        int parking_lot_id FK
        varchar code
        varchar name
        int display_order
    }

    PARKING_SLOTS {
        int id PK
        int parking_floor_id FK
        varchar code
        varchar vehicle_type
        varchar status
        boolean is_active
        timestamptz updated_at_utc
    }
```

## Khóa và ràng buộc

### parking.parking_lots

- PK: `id` (integer identity)
- Unique: `code`
- Check: `code`, `name` và `address` không rỗng
- Check: `latitude` null hoặc trong [-90, 90]
- Check: `longitude` null hoặc trong [-180, 180]
- Index: `is_active`

Latitude/Longitude để nullable vì chưa có tọa độ thật. Không seed GPS giả.

### parking.parking_floors

- PK: `id`
- FK: `parking_lot_id` → `parking_lots.id` (Restrict, không xóa bãi khi còn tầng)
- Unique: (`parking_lot_id`, `code`)
- Check: `code`/`name` không rỗng, `display_order >= 0`

Layout bản đồ chưa xác định nên chưa thêm cột layout.

### parking.parking_slots

- PK: `id`
- FK: `parking_floor_id` → `parking_floors.id` (Restrict, không xóa tầng khi còn chỗ)
- Unique: (`parking_floor_id`, `code`)
- Check `vehicle_type`: `car`, `moto`, `ev`
- Check `status`: `available`, `occupied`, `reserved`, `out_of_service`
- Index: `status`, `is_active`

Không sinh slot từ `availableSlots` của API JSON.

## Ranh giới dữ liệu

Chỉ module Parking được ghi các bảng `parking_*`. Module khác không ghi trực tiếp các bảng này.

API hiện tại:

- `GET /parking-lots` đọc JSON mẫu
- `GET /api/ParkingSlots` đọc mock trong memory

Chưa map JSON sang PostgreSQL.

## Schema dự kiến phát triển sau

Không tạo trong Migration đầu tiên.

```mermaid
erDiagram
    IDENTITY_USERS ||--o{ IDENTITY_VEHICLES : "sở hữu"
    IDENTITY_ROLES ||--o{ IDENTITY_USERS : "gán quyền"
    PARKING_SLOTS ||--o{ BOOKING_HOLDS : "giữ chỗ"
    BOOKING_HOLDS ||--o| BOOKINGS : "thành booking"
    BOOKINGS ||--o{ PAYMENT_TRANSACTIONS : "thanh toán"
    PARKING_SLOTS ||--o{ IOT_DEVICES : "gắn thiết bị"
    IOT_DEVICES ||--o{ IOT_SENSOR_EVENTS : "ghi nhận"
    BOOKINGS ||--o{ RECOGNITION_EVENTS : "đối soát biển số"

    IDENTITY_USERS {
        int id PK
        string email
        string role
    }

    IDENTITY_ROLES {
        int id PK
        string name
    }

    IDENTITY_VEHICLES {
        int id PK
        int user_id FK
        string plate_number
    }

    BOOKING_HOLDS {
        int id PK
        int slot_id FK
        timestamptz expire_at
    }

    BOOKINGS {
        int id PK
        int user_id FK
        int slot_id FK
        string status
    }

    PAYMENT_TRANSACTIONS {
        int id PK
        int booking_id FK
        decimal amount
        string status
    }

    IOT_DEVICES {
        int id PK
        int slot_id FK
        string device_code
    }

    IOT_SENSOR_EVENTS {
        int id PK
        int device_id FK
        string payload
    }

    RECOGNITION_EVENTS {
        int id PK
        int booking_id FK
        string plate_number
    }
```

Phụ trách dự kiến:

- `identity`, `parking`, `booking`: Phụng
- `payment`: Nam Anh
- `iot`: Thức
- `recognition`: Đức, nếu cần
- Hạ tầng dùng chung: Trí
- Kiểm thử: Danh
- REST API cho Frontend: Song
