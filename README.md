# GTA IV Offline Mod Launcher

Launcher WPF giúp cài xe add-on cho **GTA V Offline / Story Mode** nhanh hơn, không cần thao tác tay nhiều trong OpenIV.

## Chức năng chính

- Chọn thư mục GTA V bằng cách kiểm tra `GTA5.exe`
- Chọn đúng file mod `dlc.rpf`
- Tự copy mod vào:
  - `{GtaPath}\mods\update\x64\dlcpacks\{ModName}\dlc.rpf`
- Tự chuẩn bị file:
  - `{GtaPath}\mods\update\update.rpf`
- Tự cập nhật `dlclist.xml` trong `mods\update\update.rpf`
- Tự backup trước khi sửa file vào:
  - `{GtaPath}\mods_launcher_backups`
- Lưu danh sách mod đã cài tại:
  - `%AppData%\GTAVOfflineModLauncher\installed_mods.json`
- Gỡ mod đã chọn
- Mở game bằng launcher

## Cách dùng nhanh

1. Mở app
2. Bấm **Browse GTA Folder** và chọn thư mục game
3. Bấm **Select dlc.rpf** và chọn file mod
4. Nhập:
   - **Mod folder name**
   - **Spawn name**
5. Bấm **Install Mod**

Sau khi cài, app sẽ tự:
- copy `dlc.rpf` vào thư mục `mods`
- tạo hoặc dùng `mods\update\update.rpf`
- thêm dòng DLC vào `dlclist.xml`

## Gỡ mod

1. Chọn mod trong danh sách đã cài
2. Bấm **Uninstall Mod**

## Lưu ý

- App chỉ làm việc với **mod dạng `dlc.rpf`**
- App chỉ sửa trong thư mục `mods`, không sửa trực tiếp file gốc của game
- Nếu game hoặc file mod không đúng cấu trúc, app sẽ báo lỗi

## Build

```powershell
dotnet build
```

## Chạy app

```powershell
dotnet run
```

## Công nghệ

- C#
- .NET 8
- WPF
- CodeWalker.Core
