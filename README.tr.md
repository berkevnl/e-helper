<p align="center">
  <img src="docs/cover.png" alt="E-Helper — Casper Excalibur Dizüstü Bilgisayarlar İçin Hafif Kontrol Merkezi" width="100%">
</p>

# E-Helper — Casper Excalibur Kontrol Merkezi

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest"><img src="https://img.shields.io/github/v/release/berkevnl/E-Helper?style=flat-square&color=0078D4&label=S%C3%BCr%C3%BCm" alt="Sürüm"></a>
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip"><img src="https://img.shields.io/badge/%C4%B0ndir-ZIP_Paketi_(~64_MB)-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Doğrudan ZIP İndir"></a>
  <a href="https://github.com/berkevnl/E-Helper/blob/main/LICENSE"><img src="https://img.shields.io/badge/Lisans-GPL--3.0-green.svg?style=flat-square" alt="Lisans"></a>
  <a href="#-neden-e-helper-temel-avantajlar"><img src="https://img.shields.io/badge/RAM-5--10_MB_Tepsi_/_30--50_MB_Aktif-success?style=flat-square" alt="RAM Tüketimi"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0_Windows_x64-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8"></a>
  <a href="README.md"><img src="https://img.shields.io/badge/Language-English-blue?style=flat-square" alt="English Documentation"></a>
</p>

---

**E-Helper**, **Casper Excalibur** serisi oyuncu dizüstü bilgisayarları için üreticinin yüksek kaynak tüketen, kararsız ve hantal kontrol yazılımlarına (*Excalibur Control Center*) alternatif olarak sıfırdan geliştirilmiş, açık kaynaklı ve ultra hafif bir donanım yönetim merkezidir. Kurulum gerektirmeyen tek bir taşınabilir çalıştırılabilir dosyadan ibarettir.

ASUS ekosisteminde devrim yaratan **G-Helper** felsefesi temel alınarak geliştirilen E-Helper; hiçbir arka plan servisi kurmadan, hiçbir telemetri takipçisi barındırmadan ve sistemi asla yormadan doğrudan anakartın gömülü denetleyicisi (EC) ve ACPI/WMI alt sistemiyle haberleşir.

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip">
    <img src="https://img.shields.io/badge/GÜNCEL_SÜRÜMÜ_İNDİR-ZIP_PAKETİ_(~64_MB)-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="E-Helper ZIP Paketini İndir" height="46">
  </a>
  <br>
  <sub><i>Sıkıştırılmış hafif taşınabilir paket (~64 MB) — Arşivden çıkarın ve kurulum yapmadan doğrudan çalıştırın.<br>Sıkıştırılmamış tek dosya (.exe) ve sürüm geçmişi için <a href="https://github.com/berkevnl/E-Helper/releases/latest">GitHub Sürümler (Releases)</a> sayfasını ziyaret edebilirsiniz.</i></sub>
</p>

---

## 📊 Orijinal Control Center ile E-Helper Karşılaştırması

| Özellik / Kriter | Orijinal Üretici Yazılımı | E-Helper (v1.3.1) |
| :--- | :---: | :---: |
| **RAM Tüketimi (Tepside / Boşta)** | 150 MB – 350 MB (Bellek sızıntılarıyla 450 MB+ üzerine çıkabilir) | **5 MB – 10 MB** |
| **RAM Tüketimi (Pencere Açıkken)** | 250 MB – 500 MB | **30 MB – 50 MB** (Otomatik bellek kümeleme optimizasyonu) |
| **Sürekli Çalışan Arka Plan Servisleri** | 4 – 6 sürekli çalışan Windows servisi (`ControlCenter.exe`, `FnHotkeyUtility`, `OSDService`, `GCUBridge`) | **0 (Sıfır arka plan servisi)** |
| **Telemetri ve Kullanım Takibi** | Arka planda telemetri ve analitik veri toplama açık | **0 (Tamamen yerel, sıfır takip)** |
| **Açılış Tepki Süresi** | 3 – 6+ saniye (hantal web/hibrit motor) | **Anında (< 100 ms açılış hızı)** |
| **Kurulum Gereksinimi** | Çok adımlı yükleyici, sürücü servisleri, kayıt defteri yükü | **Tek dosya taşınabilir paket (Kurulumsuz)** |
| **Açılışta Ayar Korunumu** | Yeniden başlatma veya uykudan uyanmada fan/RGB ayarlarını sıkça sıfırlar | **%100 kararlı açılış sonrası gecikmeli EC senkronizasyonu** |
| **Donanım İletişim Mimarisi** | Araya giren hantal servis katmanları ve süreçler arası iletişim (IPC) | **Doğrudan ACPI WMI SMI (`0xFA00` / `0xFB00`) ve NVIDIA NVML** |
| **Klavye Aydınlatma Denetimi** | Temel üretici modları | **3-Bölge Bağımsız Renk, Senkronize Döngü (70 FPS), Soldan Sağa Dalga, 3 Kademeli Hız Barı** |
| **Yazılım Güncellemeleri** | Manuel indirme ve yeniden kurulum zorunluluğu | **Tek tıkla sessiz arka plan kontrolü, indirme ve yeniden başlatma** |

---

## ⚡ Neden E-Helper? (Temel Avantajlar)

1. **Casper Excalibur Donanımına Özel:** Yalnızca Casper Excalibur dizüstü bilgisayarlarının anakart yapısına ve Quanta gömülü denetleyici (EC) yazmaçlarına odaklanılarak geliştirilmiştir; genel donanım karmaşası içermez.
2. **Mikroskobik Kaynak Tüketimi:** Sistem tepsisinde beklerken yalnızca **5 – 10 MB RAM** harcar. Arayüz ekranda açıkken dahi otomatik çalışan kümeleme optimizasyonu (*SetProcessWorkingSetSize*) sayesinde bellek kullanımı kesin olarak **30 – 50 MB** aralığında tutulur.
3. **Sıfır Arka Plan Yükü (Bloat-Free):** Arka planda çalışan servisler (services), telemetri takipçileri ve işlemciyi yoran gereksiz süreçler barındırmaz; boştayken %0 CPU tüketir.
4. **Kalıcı Ayar Korunumu:** Seçilen güç profili, klavye modu, animasyon hızı, parlaklık seviyesi ve bölge renkleri; bilgisayar kapatılıp açıldığında veya yeniden başlatıldığında kaybolmaz. Windows Görev Zamanlayıcısı başlangıç entegrasyonu ve gecikmeli EC senkronizasyonu ile otomatik olarak geri yüklenir.
5. **Doğrudan Donanım Köprüsü:** Üreticinin hantal servisleri yerine doğrudan anakart gömülü denetleyicisiyle (`0xFA00` Okuma / `0xFB00` Yazma SMI) ve harici ekran kartı için `nvml.dll` ile yerel iletişim kurar.
6. **Kompakt Sistem Tepsisi Paneli:** Görev çubuğunda kalabalık yapmaz, bildirim alanındaki simgesine tıklandığında anında fırlayan modern bir panel açılır ve istendiğinde ekranda serbestçe taşınabilir.
7. **Çift Dil & Çift Tema Desteği:** Tek tıkla Türkçe ve İngilizce dilleri arasında, ayrıca G-Helper esintili Mat Koyu ve Ferah Açık tema seçenekleri arasında anında geçiş yapılabilir.

---

## 🎯 Özellikler ve Yetenekler

### 1. ⚡ Güç ve Performans Modları
* **Tasarruf (Eco / Office):** Düşük fan devri, sessiz akustik profil ve maksimum pil ömrü sunarak ofis çalışmalarında ve günlük kullanımda sessizlik sağlar.
* **Dengeli (Balanced / Gaming):** Dinamik fan eğrisi ve dengeli termal yönetim ile günlük çoklu görev ve dengeli oyun seansları için idealdir.
* **Performans (Turbo / Performance):** Maksimum fan devrini serbest bırakır, termal tavanı yükseltir ve işlemci/ekran kartının en yüksek frekanslarda kararlı kalmasını sağlar.
* *Windows yerel güç planlarıyla (`PowerSetActiveScheme`) otomatik eşzamanlı çalışır.*

### 2. 📊 Canlı Donanım Telemetrisi
* **Sıcaklık ve Fan Göstergeleri:** Gerçek zamanlı işlemci (CPU) çekirdek sıcaklığı (°C) ve takometre fan devirleri (RPM).
* **Güvenilir Harici GPU Telemetrisi:** Harici ekran kartının gerçek çekirdek sıcaklığı ve fan devirleri, hatalı veya sabit değerli anakart sensörleri yerine doğrudan **NVIDIA NVML** (`nvml.dll`) sürücü arayüzünden okunur.
* **Sistem Kaynakları İzleme:** Canlı bellek (RAM) kullanımı (kullanılan / toplam GB ve yüzdelik oran) ve ana depolama diski (C:) doluluk durumu.
* **Sistem Tepsisi Canlı İpucu (Tooltip):** Fare tepsi simgesinin üzerine getirildiğinde pencereyi açmaya gerek kalmadan anlık CPU/GPU sıcaklıkları ve fan RPM bilgileri gösterilir.

### 3. 🌈 RGB Klavye Aydınlatma Motoru
* **6 Farklı Aydınlatma Modu:**
  * **Sabit (Static):** Tüm klavyede seçilen tek bir sabit renk.
  * **Nefes (Breathing):** Anakart donanım seviyesinde doğal, ışığı sıfır karanlığa kadar söndürüp geri getiren pürüzsüz soluk alma döngüsü.
  * **Dinamik (Colorful Dynamic Cycle):** Donanımsal sürekli renk değişimi.
  * **Döngü (Synchronized Spectrum Cycle):** Yüksek yenileme hızlı (~70 FPS), tüm tuşların aynı anda aynı rengi alarak 360° renk spektrumundaki tonlar arasında kesintisiz, akıcı ve basamaksız dolaştığı senkronize mod.
  * **Gökkuşağı (Rainbow Wave):** Soldan sağa doğru (WASD → Orta → Numpad) akıcı şekilde ilerleyen çok bölgeli spektrum dalgası.
  * **3-Bölge (3-Zone Custom RGB):** Klavyenin 3 bağımsız bölgesine ayrı ayrı renk atayabilme imkanı.
* **İnteraktif 2D 3-Bölge Haritası:** Kuşbakışı 2D 3 parçalı klavye şematiği ile Sol (WASD), Orta ve Sağ (Numpad) alanlarına doğrudan tıklayarak renk atayabilme.
* **Dinamik 3 Kademeli Hız Barı:** Döngü ve Gökkuşağı modlarında animasyon hızını (**Yavaş**, **Orta**, **Hızlı**) anında ayarlayabilme; seçili hızın başlıkta canlı gösterimi (`Parlaklık: %100 - Hızlı`).
* **Windows 11 Tarzı Renk Seçici:** 2D HSV renk alanı, 1D ton kaydırıcısı, Hex kodu girişi, RGB sayısal kontrolleri ve hazır renk paleti ile eksiksiz renk seçimi.
* **Donanımsal Parlaklık Kademeleri:**
  * `0` : **Kapalı (%0)** — Klavye LED güç hattını donanımsal olarak kapatır.
  * `1` : **%50 Parlaklık** — Dengeli ortam aydınlatması.
  * `2` : **%100 Parlaklık** — Tam parlaklık seviyesi.
  * *Kaydırıcı çubuğun istenen noktasına tıklanarak doğrudan istenen kademeye geçilebilir.*
* **Yeniden Başlatmada Korunum:** Bilgisayar yeniden başlatıldığında klavye modunuz, parlaklığınız, animasyon hızınız ve bölge renkleriniz otomatik olarak geri yüklenir.
* **Akıllı Boşta Kalma (Uyku) Zamanlayıcısı:** RGB kartının sağ üst köşesindeki sayaç simgesinden 15s, 30s, 45s, 60s veya 5dk seçilerek boşta kalındığında klavye aydınlatmasının otomatik kapanması; klavye tuşuna, fareye veya touchpad'e dokunulduğu anda son ayarlarla otomatik uyanması.

### 4. 🎨 Arayüz ve Görsel Ergonomi
* **Modern Tipografi:** Google Sans ve modern sistem yazı tipleri; ClearType ve Display alt piksel işleme modları ile tüm monitörlerde jilet gibi keskin metin görünümü.
* **%100 Vektörel İkonlar:** Kırpılma veya DPI ölçekleme bozulması yaşamayan modern vektörel ikonlar.
* **Tek Tıkla Tema Değişimi:** Mat Koyu ve Ferah Açık tema seçenekleri.
* **Asenkron Arayüz Mimarisi:** Donanım okuma ve yazma işlemleri kullanıcı arayüzü iş parçacığından tamamen bağımsız çalışır; arayüz asla donmaz veya takılmaz.

### 5. 🔄 Dikişsiz Otomatik Güncelleyici
* **Sıfır Gecikmeli Arka Plan Kontrolü:** E-Helper her açılışta arka planda GitHub Releases üzerinden en güncel sürümü kontrol eder; açılış hızını asla etkilemez (0 ms gecikme).
* **Etkileşimli Sürüm Durumu:** Sol alt köşede anlık sürüm bilgisi (Uygulama güncelse gri `Sürüm: 1.3.1`, güncelleme varsa kırmızı `İndir: 1.3.0 → 1.3.1`).
* **Tek Tıkla Otomatik Güncelleme:** Güncelleme bağlantısına tıklandığında son sürüm paketi arka planda otomatik olarak indirilir, açık olan uygulama kapatılır, çalıştırılabilir dosya yenilenir ve yeni sürüm anında başlatılır.

---

## 💻 Donanım Uyumluluğu

E-Helper, Quanta ACPI/WMI Gömülü Denetleyici altyapısını kullanan **Casper Excalibur** modelleri için özel olarak hazırlanmıştır:

### ✅ Fiziksel Donanımda Test Edilen Modeller
> Test edilmiş ve tüm işlevleriyle (güç profilleri, telemetri, fan devirleri ve RGB klavye aydınlatması) kusursuz çalıştığı onaylanmıştır:
* **Excalibur G770** (Test Edildi & Doğrulandı)
* **Excalibur G850 / G860** (Test Edildi & Doğrulandı)
* **Excalibur G870** (Test Edildi & Doğrulandı)
* **Excalibur G911** (Test Edildi & Doğrulandı)

### 🔍 Mimari Olarak Uyumlu Modeller
Quanta ACPI/WMI komut setini paylaşan Casper Excalibur modelleriyle uyumludur:
* **Excalibur G780**
* **Excalibur G900**
* **Excalibur G920**

---

## 🔬 Çalışma Mantığı ve Mimari

```
┌────────────────────────────────────────────────────────────────────────┐
│                        E-Helper (WPF / .NET 8)                         │
│            Flyout Panel  •  Sistem Tepsisi  •  Ayar Motoru             │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
                    ▼ (ACPI WMI SMI)                 ▼ (NVIDIA NVML)
        ┌────────────────────────┐      ┌────────────────────────┐
        │  Casper / Quanta EC    │      │  NVIDIA Harici GPU     │
        │  - Güç Profilleri      │      │  - Gerçek Çekirdek Isı │
        │  - Fan Devirleri (RPM) │      │  - Harici Fan Devri    │
        │  - RGB Klavye Hatları  │      └────────────────────────┘
        └────────────────────────┘
```

1. **WMI ACPI SMI Köprüsü:** Anakart üzerindeki gömülü denetleyiciyle `ROOT\WMI` isim alanı üzerinden doğrudan haberleşir. Donanım komutları standart `SMI_STRUCT_S` paketlerine sarılır:
   * `0xFA00`: Okuma Komutu (sıcaklık sensörleri ve fan RPM takometreleri).
   * `0xFB00`: Yazma Komutu (performans modları, fan hızı zorlamaları ve LED aydınlatma paketleri).
   * Alt fonksiyonlar: `0x0100` (LED aydınlatma), `0x0200` (CPU/GPU sıcaklıkları), `0x0205` (Fan hızları), `0x0300` (Güç modu).
2. **NVML Doğrudan Bağlantısı:** `nvml.dll` kütüphanesini dinamik olarak yükleyerek NVIDIA GeForce ekran kartı sürücüsünden doğrudan doğruya termal verileri okur; anakarttaki kalibre edilmemiş sensörleri devre dışı bırakır.
3. **Yetkilendirilmiş Görev Zamanlayıcısı:** Windows Görev Zamanlayıcısı üzerinde en yüksek yetkiyle (`HighestAvailable`) bir başlangıç görevi oluşturur; bu sayede bilgisayar açılışında UAC (Kullanıcı Hesabı Denetimi) uyarısı vermeden doğrudan sistem tepsisinde başlar.
4. **Otomatik Bellek Kümeleme (Working-Set Trimming):** Panel tepsiye küçüldüğünde Windows `SetProcessWorkingSetSize` API'sini (`MemoryOptimizer`) tetikleyerek kullanılmayan fiziksel bellek sayfalarını işletim sistemine geri iade eder.

---

## 📂 Proje Yapısı

```
ehelper/
├── src/
│   └── EHelper/
│       ├── Core/                      # Alan arayüzleri, modeller ve sabitler
│       │   ├── Enums/                 # PowerMode, LightingMode, BrightnessStep, ThemeMode
│       │   ├── Interfaces/            # IHardwareProvider, ISmiHardwareProvider sözleşmeleri
│       │   └── Models/                # HardwareTelemetry, HardwareCapabilities modelleri
│       ├── Hardware/                  # Düşük seviyeli donanım soyutlama katmanı
│       │   ├── Casper/                # ACPI WMI SMI ve NVIDIA NVML uygulamaları
│       │   │   ├── ExcaliburBridge.cs     # Anakart EC & WMI SMI haberleşme köprüsü
│       │   │   ├── ExcaliburSmi.cs        # SMI_STRUCT_S C/C++ interop yapıları ve P/Invoke
│       │   │   ├── NvmlHelper.cs          # Doğrudan NVIDIA NVML GPU telemetri entegrasyonu
│       │   │   └── ExcaliburPowerPlan.cs  # Windows Güç Planı eşitleme servisi
│       │   └── Simulation/            # Geliştirme ve test için sahte (mock) donanım sağlayıcısı
│       ├── Services/                  # Uygulama çalışma zamanı servisleri
│       │   ├── HardwareMonitorService.cs  # Asenkron arka plan telemetri döngüsü
│       │   ├── ConfigManager.cs           # Kullanıcı ayarları kalıcılığı (config.json)
│       │   ├── StartupManager.cs          # Windows Görev Zamanlayıcısı başlangıç entegrasyonu
│       │   ├── UpdateService.cs           # GitHub Releases sessiz otomatik güncelleyici
│       │   ├── MemoryOptimizer.cs         # Otomatik RAM bellek temizleyici (TrimMemory)
│       │   └── LocalizationManager.cs     # Dinamik Türkçe/İngilizce dil yöneticisi
│       ├── UI/                        # Görsel katman (WPF / XAML)
│       │   ├── MainWindow.xaml            # Modern flyout paneli ve 2D klavye şematiği
│       │   ├── ColorPickerWindow.xaml     # Windows 11 tarzı HSV RGB renk seçici
│       │   └── TrayIcon.cs                # Sistem tepsisi simgesi ve canlı ipucu (tooltip)
│       └── App.xaml                   # Uygulama giriş noktası ve genel kaynaklar
├── scripts/                           # Derleme ve dijital imzalama betikleri
│   └── build-and-sign.ps1             # Otomatik paketleme ve imzalama aracı
├── docs/                              # Dökümantasyon görselleri ve kapak resmi
│   └── cover.png                      # Uygulama tanıtım afişi
├── LICENSE                            # GNU Genel Kamu Lisansı v3.0 (GPL-3.0)
└── E-Helper.slnx                      # Modern Visual Studio çözüm dosyası
```

---

## 🚀 İndirme ve Hızlı Başlangıç

1. En güncel **[`E-Helper-win-x64.zip`](https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip)** paketini indirin (~64 MB sıkıştırılmış boyut).
2. ZIP arşivini dilediğiniz bir klasöre çıkartın (Örn: `C:\Program Files\E-Helper` veya kişisel kullanıcı klasörünüz).
3. `E-Helper.exe` dosyasına sağ tıklayıp **Yönetici Olarak Çalıştır**'ı seçin *(Windows'ta ACPI WMI donanım yönetimine erişebilmek için yönetici yetkisi zorunludur)*.
4. E-Helper ekranın sağ altındaki **Sistem Tepsisi**'ne (saatin yanına) yerleşecektir. Simgeye tıklayarak kontrol panelini açabilirsiniz!
5. Uygulama içerisinden **Windows ile Başlat** seçeneğini işaretleyerek Windows açılışında UAC uyarısı vermeden otomatik başlamasını sağlayabilirsiniz.

> [!TIP]
> **Neden ZIP paketi indirmelisiniz?**
> Sıkıştırılmış ZIP paketi **~64 MB** boyutuyla, ~156 MB'lık sıkıştırılmamış tekil exe dosyasına kıyasla çok daha az internet kotası tüketir ve kat kat hızlı iner. İçerisinde gömülü .NET çalışma zamanını eksiksiz barındırır.

---

## 🛠️ Kaynak Koddan Derleme

### Gereksinimler
* Windows 10 / 11 (64-bit)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) veya üzeri
* Visual Studio 2022 (*.NET Masaüstü Geliştirme* iş yükü ile) veya VS Code / JetBrains Rider

### Derleme Adımları
```powershell
# Projeyi klonlayın
git clone https://github.com/berkevnl/E-Helper.git
cd E-Helper

# Bağımlılıkları geri yükleyin ve Release derlemesi yapın
dotnet build src/EHelper/EHelper.csproj -c Release

# Veya bağımsız tek dosya ve ZIP paketlerini oluşturun
powershell -ExecutionPolicy Bypass -File scripts/build-and-sign.ps1
```

Derlenen bağımsız çalıştırılabilir dosya ve dağıtım ZIP arşivi `publish-standalone\` klasörü altına oluşturulacaktır.

---

## 💖 Teşekkür ve Felsefe

Bu proje, **[Seerge](https://github.com/seerge)** tarafından geliştirilen ve ASUS ekosisteminde standart haline gelen efsanevi **[G-Helper](https://github.com/seerge/g-helper)** projesinden derinden ilham almıştır. E-Helper; aynı hafif, sade ve kullanıcıya saygılı deneyimi doğrudan Casper Excalibur kullanıcılarına sunmayı amaçlamaktadır.

---

## ⚖️ Marka ve Yasal Sorumluluk Reddi

* Bu dökümanda ve uygulamada adı geçen tüm ürün adları, logolar ve markalar (*Casper*, *Excalibur*, *Windows*, *NVIDIA* vb.) ilgili hak sahiplerinin mülkiyetindedir ve yalnızca donanım uyumluluğunu belirtmek amacıyla kullanılmıştır.
* **E-Helper**, topluluk tarafından geliştirilen bağımsız bir açık kaynak projesidir; Casper Bilgisayar Sistemleri A.Ş. ile herhangi bir resmi bağı, ortaklığı veya sponsorluğu bulunmamaktadır.
* Bu yazılım **GNU Genel Kamu Lisansı v3.0 (GPL-3.0)** kapsamında faydalı olması ümidiyle dağıtılmaktadır; ancak HİÇBİR GARANTİ verilmemektedir.
