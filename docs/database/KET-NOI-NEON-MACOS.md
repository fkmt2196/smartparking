# Dùng DB Neon chung trên Mac

## 1. Cài .NET SDK 10 nếu máy chưa có

Tải tại https://dotnet.microsoft.com/download/dotnet/10.0, chọn **SDK cho macOS**:

- Mac chip Apple M1/M2/M3/M4...: **Arm64**.
- Mac chip Intel: **x64**.

Kiểm tra bằng `dotnet --list-sdks`; cần có dòng `10.0.x`. Không cần cài PostgreSQL hoặc Docker.

## 2. Kết nối và chạy backend

Mở Terminal ở thư mục `smartparking` có `backend` và `scripts`, rồi chạy:

```bash
bash scripts/connect-neon-macos.sh --run
```

Nhập mật khẩu DB Neon do người phụ trách gửi riêng rồi Enter. Mật khẩu không hiện ký tự khi nhập. Nếu được cấp user DB riêng, thêm `--username TEN_USER` vào lệnh.

Script dùng host của nhóm và database `smartparking`, kiểm tra bằng truy vấn lịch sử migration thật, rồi mới lưu User Secrets trên Mac và chạy backend. Không tự tạo/xóa bảng hoặc chạy migration cập nhật DB.

Khi thấy **`DA XAC MINH KET NOI DB NEON CUA NHOM: smartparking`**, máy đã truy vấn được DB với cấu hình đó. Nếu có migration chưa áp dụng, báo người phụ trách DB; đây là việc riêng với cấu trúc bảng.

## 3. Những lần sau

Chạy backend bằng cấu hình đã lưu:

```bash
env -u ConnectionStrings__Parking dotnet run --project backend/src/Parking.WebAPI --launch-profile http
```

Backend chạy tại `http://localhost:5174`. Muốn kiểm tra lại kết nối hoặc thay mật khẩu, chạy lại lệnh ở bước 2.

Nếu lỗi mật khẩu/kết nối, script dừng trước khi lưu cấu hình. Host trong script lấy từ ảnh Connect của nhóm; người phụ trách cần cập nhật nếu endpoint thay đổi. Các API hiện dùng JSON/mock vẫn cần tích hợp PostgreSQL để dữ liệu trên giao diện được dùng chung.

Tham khảo: [Cài .NET trên macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos), [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).
