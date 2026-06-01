# Nythera: Image Wallpaper & Management Platform Integration

Bu plan, Nythera'yı yalnızca bir video duvar kağıdı motoru olmaktan çıkarıp kapsamlı bir **Wallpaper Management Platform**'a dönüştürmek için gerekli "Resim Seçimi (Image Page)" ve görsel düzenleme özelliklerinin (MVP) entegrasyonunu içerir. Geri bildirimler doğrultusunda güncellenmiştir.

## Kararlaştırılan Mimari ve Özellikler
> [!IMPORTANT]
> **Efekt Motoru:** MVP sürümünde **WebView2** kullanılacaktır. Bu sayede CSS ve Javascript ile Ken Burns ve Parallax gibi efektler performanslı ve hızlı bir şekilde geliştirilecektir.
> 
> **Carousel Sıralaması:** Kullanım sıklığı göz önüne alınarak yeni sıralama şu şekilde olacaktır: `Images -> Videos -> Marketplace -> Settings -> About`.
>
> **MVP Özellikleri:** Fill, Fit, Stretch, Center ve **Span** (çoklu monitör) yerleşimleri eklenecektir. Filtre olarak Brightness, Blur, Contrast ve animasyon olarak Ken Burns, Parallax MVP'ye dahil edilecek; Snow, Rain, Audio Reactive gibi özellikler sonraki sürümlere bırakılacaktır.
>
> **Playlist:** Resimler için Slayt Gösterisi (Playlist) özelliği, zamanlayıcı seçenekleri (5 dk, 15 dk, 30 dk vb.) ile eklenecektir.

## Proposed Changes

### 1. Carousel & UI Güncellemeleri (`MainPage.xaml` & `MainPage.xaml.cs`)
Carousel yapısı ve sıralaması yeniden düzenlenecek.
#### [MODIFY] [MainPage.xaml](file:///c:/Users/sahil/source/cursor-ai/walpaper-for-pc/Nythera/MainPage.xaml)
- **Sayfa Sıralaması:** Resim sayfası ana sayfa (ilk görünen) olacak şekilde sıralama güncellenecektir (Images, Videos, Marketplace, Settings, About). PipsPager sayısı güncellenecektir.
- Yeni bir `Grid` (ör. `PageImages`) eklenecek.
- Tasarım Video sayfasına benzer olacak ve şu özellikleri içerecek:
  - **Filtreleme:** All, Favorites, Custom.
  - **Yerleşim (Layout):** Fill, Fit, Stretch, Center, Span.
  - **Efekt Slayder'ları:** Brightness, Blur, Contrast.
  - **Hareket (Animation) Toggle'ları:** Ken Burns, Parallax.
  - **Playlist (Slayt Modu):** Videolardaki playlist mantığı ve zamanlayıcı (Change Every) kontrolleri eklenecek.

#### [MODIFY] [MainPage.xaml.cs](file:///c:/Users/sahil/source/cursor-ai/walpaper-for-pc/Nythera/MainPage.xaml.cs)
- Carousel animasyonları ve sıralama mantığı güncellenecek.
- Resimlerin taranması, Playlist yönetimi ve özelliklerin (Blur, Ken Burns vb.) UI üzerinden WallpaperWindow'a aktarılması sağlanacak.

### 2. Veri Modelleri ve Servisler
#### [NEW] [Core/WallpaperImage.cs](file:///c:/Users/sahil/source/cursor-ai/walpaper-for-pc/Nythera/Core/WallpaperImage.cs)
- İlerideki Marketplace vizyonuna uygun olarak gelişmiş bir veri modeli eklenecektir:
  `Id`, `Name`, `ImagePath`, `Thumbnail`, `IsFavorite`, `IsCustom`, `IsMarketplace`, `Category`, `AddedDate`, `Blur`, `Brightness`, `Contrast`, `EnableKenBurns`, `EnableParallax`, `EnableSnow`, `EnableRain`, `LayoutMode`.
#### [MODIFY] [Services/SettingsService.cs](file:///c:/Users/sahil/source/cursor-ai/walpaper-for-pc/Nythera/Services/SettingsService.cs)
- Seçili resimlerin, playlist yapılandırmasının ve her bir resme ait gelişmiş efekt ayarlarının kaydedilmesi sağlanacak.

### 3. Arka Plan Çizim Motoru (`WallpaperWindow.xaml` & `WallpaperWindow.xaml.cs`)
#### [MODIFY] [WallpaperWindow.xaml](file:///c:/Users/sahil/source/cursor-ai/walpaper-for-pc/Nythera/WallpaperWindow.xaml)
- `<MediaPlayerElement>`'in yanına WebView2 paketi (Microsoft.Web.WebView2) kurularak `<WebView2 x:Name="BackgroundWebView" />` kontrolü eklenecek.
- Kullanıcı Video seçtiğinde MediaPlayerElement, Resim seçtiğinde WebView2 gösterilecek.

### 4. Gelişmiş Efektler (WebView2 İçeriği)
#### [NEW] Assets/ImageEngine/
- WebView2'de çalıştırılmak üzere `index.html`, `style.css` ve `engine.js` dosyaları oluşturulacak.
- **engine.js / style.css:** Resmin Layout mode'u (Span, Fit, Fill) ayarlanacak. Blur/Brightness/Contrast CSS filtreleri olarak uygulanacak. Ken Burns efekti için CSS keyframes, Parallax için mouse/imleç (veya fake) hareketi üzerinden CSS transform eklenecektir.

## Verification Plan

### Automated Tests
- WebView2 Runtime'ın mevcudiyetinin kontrolü ve yoksa uygun hatanın/uyarının verilmesi.
- `WallpaperImage.cs` üzerinden serialize/deserialize işlemlerinin (Ayarların) test edilmesi.

### Manual Verification
- Carousel'in yeni `Images -> Videos -> ...` sıralamasında sorunsuz çalıştığı test edilecek.
- Playlist zamanlayıcısının belirlenen aralıklarda (ör. 1 dakika) resimleri değiştirdiği doğrulanacak.
- WebView2 üzerinden Parallax ve Ken Burns efektlerinin performans kaybı olmadan çalıştığı ve resim değiştirildiğinde efektlerin doğru şekilde yeniden yüklendiği gözlemlenecek.
