# Nora Wallpaper - Modern Video Duvar Kağıdı Uygulaması Planı

Bu belge, Windows 11 hissiyatını yansıtan, düşük sistem kaynağı tüketen, modern ve anime odaklı bir canlı duvar kağıdı uygulamasının (MVP) teknik mimarisini ve geliştirme yol haritasını içermektedir.

> [!TIP]
> **Vizyon:** Wallpaper Engine'in karmaşıklığından uzak, kurulumu ve kullanımı basit, modern arayüzlü ve özellikle **video (MP4/WebM)** formatlarına odaklanan hafif bir alternatif yaratmak.

## 1. Teknoloji Yığını (Tech Stack)

Başlangıç için en ideal, performanslı ve Windows ekosistemine en uygun teknolojiler seçilmiştir:

*   **Platform & Dil:** C# ve .NET 8 (veya en güncel sürüm). Windows API'lerine doğrudan ve güvenli erişim, yüksek performans.
*   **Kullanıcı Arayüzü (UI):** **WinUI 3 (Windows App SDK)**. Native Windows 11 görünümü (Mica, Fluent Design), düşük RAM tüketimi ve akıcı animasyonlar.
*   **Video Render Motoru:** **LibVLCSharp** veya **MPV.NET**. Donanım hızlandırması (GPU acceleration) sunarak işlemciye yük bindirmeden video oynatımı sağlar.
*   **Native Entegrasyon:** Windows API (PInvoke) kullanılarak WorkerW (Desktop Layer) arkasına pencere yerleştirme işlemi.

## 2. Mimari Yapı (Klasör ve Modül Organizasyonu)

Uygulamanın sürdürülebilir olması için Clean Architecture prensiplerine yakın, modüler bir yapı kurulacaktır:

```text
NoraWallpaper/
│
├── 📁 NoraWallpaper.App/ (Ana Başlangıç Projesi - WinUI 3)
│   ├── 📁 UI/
│   │   ├── 📁 Pages/       (Dashboard, Settings, Gallery vb.)
│   │   ├── 📁 Components/  (Video Kartları, Butonlar, Modal'lar)
│   │   └── 📁 Themes/      (Renk paletleri, Fluent stilleri)
│   └── App.xaml
│
├── 📁 NoraWallpaper.Core/ (İş Mantığı ve Yöneticiler)
│   ├── 📁 WallpaperEngine/ (WorkerW hack'i ve pencere yönetimi)
│   ├── 📁 VideoPlayer/     (VLC/MPV wrapper sınıfları)
│   ├── 📁 MonitorManager/  (Çoklu ekran tespiti ve yönetimi)
│   └── 📁 PerformanceManager/(Oyun açıldığında durdurma vs.)
│
├── 📁 NoraWallpaper.Services/ (Arka Plan Servisleri)
│   ├── SettingsService     (JSON/SQLite tabanlı ayar kaydı)
│   ├── WallpaperService    (Aktif duvar kağıdını yönetme)
│   └── StartupService      (Windows açılışına ekleme)
│
├── 📁 NoraWallpaper.Native/ (Windows API Çağrıları)
│   └── WindowsApi.cs       (User32.dll vb. PInvoke tanımlamaları)
│
└── 📁 NoraWallpaper.Assets/ (Uygulama ikonları, default videolar)
```

## 3. Geliştirme Yol Haritası

### AŞAMA 1: Çekirdek MVP (Masaüstünde Video Oynatma)
*Asıl amacımız: Masaüstü ikonlarının arkasında bordersız bir pencerede video oynatabilmek.*

- `[ ]` WinUI 3 boş proje kurulumu ve çözüm (solution) yapısının oluşturulması.
- `[ ]` `Native` katmanında PInvoke ile Windows masaüstü katmanını (WorkerW) bulma ve arasına pencere enjekte etme mantığının yazılması.
- `[ ]` LibVLCSharp veya MPV kütüphanesinin projeye dahil edilmesi.
- `[ ]` Transparan, bordersız ve fullscreen bir WinUI penceresi oluşturup video render elementinin eklenmesi.
- `[ ]` Kullanıcının yerel bilgisayarından bir MP4 seçip masaüstünde döngü (loop) halinde oynatabilmesi.

### AŞAMA 2: Ürünleştirme ve Kalite
*Uygulamanın günlük kullanılabilir bir "ürün" haline gelmesi.*

- `[ ]` **Tam Ekran Algılama (PerformanceManager):** Kullanıcı bir oyuna (veya tam ekran uygulamaya) girdiğinde videoyu duraklatma (Pause) ve RAM/GPU tasarrufu sağlama.
- `[ ]` **Sistem Açılışı:** Windows ile birlikte sessiz (tray icon olarak) başlama desteği.
- `[ ]` **Çoklu Monitör Desteği:** İkinci veya üçüncü monitörleri algılama ve her birine farklı video atayabilme.
- `[ ]` Modern WinUI 3 Dashboard tasarımı (Fluent Design, Mica efekti, animasyonlu geçişler).
- `[ ]` Sesli videolar için ses seviyesi ayarı (Mute default olmalı).

### AŞAMA 3 ve 4: Gelecek Vizyonu (Şimdilik Kapsam Dışı)
- **Gelişmiş Etkileşim:** Fareye tepki veren Parallax efektleri veya Audio Visualizer.
- **İçerik Ekosistemi:** Bulut tabanlı (veya Github reposu üzerinden çekilen) Anime / Cyberpunk video galerisi entegrasyonu.
- **AI Desteği:** Gelecekte metin istemiyle (prompt) yerleşik API'ler kullanılarak anlık AI video duvar kağıdı üretimi.

---

> [!WARNING]
> **Kullanıcı Onayı Bekleniyor**
> Bu planı onaylıyorsanız, WinUI 3 projesini scaffold ederek Aşama 1 (Çekirdek MVP) için gerekli altyapıyı kurmaya ve Windows API entegrasyonuna başlayabiliriz. Planda değiştirmek veya eklemek istediğiniz bir detay var mı?
