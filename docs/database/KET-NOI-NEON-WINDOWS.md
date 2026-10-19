# Dùng DB Neon chung trên Windows

Mở PowerShell ở thư mục `smartparking`, nơi có thư mục `scripts`. Máy cần .NET SDK 10.

Nếu PowerShell báo `running scripts is disabled on this system`, chạy lệnh sau trong cửa sổ hiện tại. Nếu được hỏi xác nhận, nhập `Y` rồi Enter:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned
```

Thiết lập này chỉ có hiệu lực trong phiên PowerShell hiện tại và hết hiệu lực khi đóng cửa sổ. [Tài liệu Microsoft](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies#execution-policy-scope-and-precedence).

Lần đầu hoặc khi thay mật khẩu DB:

```powershell
.\scripts\Connect-SharedDatabase.ps1 -Configure -RunBackend
```

Nhập mật khẩu DB Neon do người phụ trách gửi riêng rồi Enter. Ký tự mật khẩu được che. Nếu được cấp user DB riêng, thêm `-Username TEN_USER` vào lệnh.

Script kiểm tra kết nối thật bằng EF, rồi mới lưu User Secrets và chạy backend. Khi hiện `DA XAC MINH KET NOI DB NEON CUA NHOM: smartparking` là máy đã truy vấn được DB với cấu hình của nhóm. Migration còn pending là vấn đề schema riêng, cần báo người phụ trách; script không tự tạo/xóa bảng.

Những lần sau:

```powershell
.\scripts\Connect-SharedDatabase.ps1 -RunBackend
```

Nếu PowerShell báo `The term ... is not recognized`, kiểm tra file bằng:

```powershell
Test-Path .\scripts\Connect-SharedDatabase.ps1
```

Kết quả phải là `True`. Nếu là `False`, máy đang thiếu file hoặc terminal chưa mở đúng thư mục.

Host được cố định trong script theo ảnh Connect của nhóm; người phụ trách cần cập nhật nếu endpoint thay đổi. Việc API JSON/mock trả dữ liệu không tự chứng minh API đó đang đọc PostgreSQL.
