# Nythera Sürüm ve Güncelleme Rehberi

Bu belge, Nythera uygulaması için yeni bir sürüm yayınlamak ve otomatik güncelleme sistemini tetiklemek istediğinizde adım adım yapmanız gerekenleri içerir. Bu kuralları atlamak, kullanıcıların güncellemeleri alamamasına veya eski kodların paketlenmesine yol açabilir.

---

## 1. Versiyon Numarasını Değiştirme (Tek Nokta)
Nythera'nın yeni mimarisinde sürüm yönetimi **tamamen otomatik** hale getirilmiştir! Artık 2-3 farklı dosyaya girip kod değiştirmenize gerek yoktur. Sürüm bilgisini yalnızca projenin ana tanım dosyasından değiştirmeniz yeterlidir:

`Nythera/Nythera.csproj` dosyasını açın ve şu satırı bulun:
```xml
<Version>1.0.0</Version> <!-- Bu satırı örneğin 1.0.1 yapın -->
```

*(Not: Siz bu dosyayı güncelleyip kodu derlediğinizde (`dotnet publish`); otomatik güncelleme sistemi (`UpdateService.cs`) ve kurulum sihirbazı (`Setup.iss`) bu yeni versiyonu compiled edilmiş `.exe` dosyasından otomatik olarak okuyacaktır!)*

---

## 2. Yeni Kodları Derleme (ÇOK ÖNEMLİ)
Uygulama kodlarında (`.cs` veya `.xaml`) yaptığınız değişikliklerin `.exe` dosyasına yansıması için Inno Setup'ı açmadan önce **mutlaka** uygulamanızı derlemelisiniz.

Terminali açın, `Nythera` klasörüne girin (`cd Nythera`) ve aşağıdaki komutu çalıştırın:
```bash
dotnet publish -c Release -r win-x64
```
**Neden yapıyoruz?** Bu komut, en son yazdığınız kodları alır, Windows 64-bit için derler ve `bin\Release\net10.0-windows10.0.26100.0\win-x64\publish` klasörünün içine yepyeni dosyalar olarak çıkartır. Inno Setup bu dosyaları kullanarak paketleme yapacaktır.

---

## 3. Kurulum Dosyasını (Setup.exe) Oluşturma
Artık en güncel derlenmiş dosyalarımız hazır olduğuna göre paketleme işlemine geçebiliriz:

1. **Inno Setup Compiler** programını açın.
2. Projenizin ana dizinindeki `Setup.iss` dosyasını programın içine sürükleyip bırakın.
3. Yukarıdaki menüden **"Compile" (Play butonu)** tuşuna basın.
4. İşlem bittiğinde, projenizin içindeki `Installer` klasöründe yepyeni bir `Nythera_Setup_v1.0.1.exe` dosyası oluşacaktır.

---

## 4. GitHub Üzerinden Yayına Alma (Auto-Update Tetikleme)
Kullanıcıların bu güncellemeyi otomatik olarak görüp indirebilmesi için yeni `.exe` dosyasını GitHub'a yüklemelisiniz:

1. `Nythera_Wallpaper` GitHub deponuza gidin.
2. Sağ taraftaki **"Releases"** bölümüne tıklayın ve **"Draft a new release"** butonuna basın.
3. **Choose a tag:** kısmına kodun içine (`UpdateService.cs`) yazdığınız versiyonun **birebir aynısını** yazın (Örneğin: `v1.0.1`) ve "Create new tag" deyin.
4. "Release title" kısmına versiyon numarasını veya güncelleme adını yazın.
5. Alt kısımdaki kutuya `Installer` klasöründeki `Nythera_Setup_v1.0.1.exe` dosyasını sürükleyip bırakarak yükleyin.
6. **"Publish release"** butonuna basın.

**Tebrikler!** Artık Nythera uygulamasını açan tüm kullanıcıların karşısına saniyeler içinde "Yeni bir güncelleme bulundu!" uyarısı çıkacak ve uygulama otomatik olarak kendini yeni sürüme yükseltecektir.
