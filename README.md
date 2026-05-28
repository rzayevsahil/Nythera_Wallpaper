# NoraWallpaper 🌸

[![WinUI 3](https://img.shields.io/badge/WinUI_3-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)

*(English below)*

NoraWallpaper, Windows işletim sistemi için geliştirilmiş, yüksek performanslı, açık kaynaklı bir **Video Duvar Kağıdı Motoru (Video Wallpaper Engine)** uygulamasıdır. WinUI 3 ve Fluent Design prensipleri kullanılarak modern bir arayüzle inşa edilmiştir.

## Özellikler (Features) 🚀
- **Video Duvar Kağıdı:** `.mp4`, `.webm` ve `.mkv` formatındaki videoları doğrudan masaüstü arka planı olarak oynatır.
- **Akıllı Performans Yönetimi (Auto-Pause):** Tam ekran bir oyuna veya uygulamaya girdiğinizde bunu otomatik olarak algılar ve videoyu duraklatır. Böylece CPU/GPU tüketimini sıfırlar ve FPS düşüşü yaşatmaz.
- **Çoklu Monitör Desteği:** Sistemdeki tüm monitörleri algılar ve seçtiğiniz videoyu her ekranın kendi çözünürlüğüne uygun şekilde ayrı ayrı konumlandırır.
- **Windows Başlangıcında Çalışma:** İsteğe bağlı olarak Windows ile birlikte başlar ve son seçtiğiniz duvar kağıdını otomatik olarak yükler.
- **Sistem Tepsisi (System Tray) Entegrasyonu:** Arka planda sessizce çalışır. Tepsiden yönetilebilir, arayüz (dashboard) kapatılsa bile video oynamaya devam eder.
- **Özelleştirilebilir Sığdırma:** Videoyu ekrana Doldur (Fill), Sığdır (Fit), Genişlet (Stretch) veya Ortala (Center) seçenekleriyle boyutlandırabilirsiniz.

---

## English 🇬🇧

NoraWallpaper is a high-performance, open-source **Video Wallpaper Engine** built specifically for Windows. It utilizes WinUI 3 and Fluent Design principles to deliver a seamless, modern, and lightweight experience.

## Key Features 🚀
- **Video Wallpapers:** Play `.mp4`, `.webm`, and `.mkv` videos directly as your desktop background.
- **Smart Performance Management (Auto-Pause):** Automatically detects when you are running a full-screen game or application and pauses the wallpaper to free up CPU/GPU resources, ensuring zero FPS drops.
- **Multi-Monitor Support:** Automatically detects all connected monitors and accurately fits the wallpaper per display.
- **Launch on Startup:** Optionally starts with Windows and auto-loads your last applied video wallpaper.
- **System Tray Integration:** Runs silently in the background. Hiding the dashboard keeps the wallpaper running smoothly.
- **Customizable Video Fit:** Choose how the video spans across your screen (Fill, Fit, Stretch, or Center).

---

## Kurulum ve Çalıştırma (Installation & Run) 🛠️

Projeyi derlemek ve çalıştırmak için sisteminizde **.NET 8.0 SDK** (veya üzeri) ve Windows App SDK bağımlılıklarının kurulu olması gerekmektedir.

```bash
# Proje klasörüne gidin (Go to the project folder)
cd NoraWallpaper

# Bağımlılıkları temizleyin ve projeyi çalıştırın (Clean and Run)
dotnet clean
dotnet run
```

*Not: Uygulama çalışırken terminal penceresini kapatırsanız işlem sonlanır. En sağlıklı kullanım için `bin` klasörüne derlenen `.exe` dosyasını çalıştırın.*

---

## Teknik Detaylar (Technical Details) ⚙️
* **Framework:** C# / .NET / WinUI 3 (Windows App SDK)
* **Desktop Interop:** `WorkerW` mantığı ile masaüstü ikonlarının arkasına pencere yerleştirilmesi için `user32.dll` PInvoke yöntemleri kullanılmıştır.
* **Görsel Optimizasyon:** Windows 11 yuvarlatılmış köşelerinden (rounded corners) kaynaklanan 1-piksel beyaz çerçeve hataları DWM API (`dwmapi.dll`) ile bypass edilmiştir.
