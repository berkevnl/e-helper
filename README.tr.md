<p align="center">
  <img src="docs/cover.png" alt="E-Helper — Casper Excalibur Dizüstü Bilgisayarlar İçin Hafif Kontrol Merkezi" width="100%">
</p>

# E-Helper — Casper Excalibur Kontrol Merkezi

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases"><img src="https://img.shields.io/badge/Sürüm-v1.1.0-0078D4?style=flat-square" alt="Sürüm v1.1.0"></a>
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper.exe"><img src="https://img.shields.io/badge/İndir-E--Helper.exe-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Doğrudan İndir"></a>
  <a href="https://github.com/berkevnl/E-Helper/blob/main/LICENSE"><img src="https://img.shields.io/badge/Lisans-GPL--3.0-green.svg?style=flat-square" alt="Lisans"></a>
  <a href="#-neden-e-helper-temel-avantajlar"><img src="https://img.shields.io/badge/RAM-1--5_MB-success?style=flat-square" alt="RAM Tüketimi"></a>
  <a href="README.md"><img src="https://img.shields.io/badge/Language-English-blue?style=flat-square" alt="English Documentation"></a>
</p>

---

**E-Helper**, **Casper Excalibur** serisi oyuncu dizüstü bilgisayarları için üreticinin resmi yüksek kaynak tüketen ve şişkin kontrol yazılımlarına (Control Center) alternatif olarak özel olarak geliştirilmiş açık kaynaklı ve ultra hafif bir donanım yönetim aracıdır.

ASUS ekosisteminde devrim yaratan **G-Helper** felsefesi temel alınarak tasarlanan E-Helper, doğrudan Casper Excalibur anakart gömülü denetleyicisiyle (ACPI WMI/SMI ve NVML) haberleşir ve akıcı, tek parça modern bir yönetim paneli sunar.

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper.exe">
    <img src="https://img.shields.io/badge/GÜNCEL_SÜRÜMÜ_İNDİR-E--Helper.exe-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="E-Helper İndir" height="46">
  </a>
  <br>
  <sub><i>Doğrudan indirme butonu başlamazsa lütfen <a href="https://github.com/berkevnl/E-Helper/releases/latest">Releases (Sürümler)</a> sayfasından son sürümü edinin.</i></sub>
</p>

---

## ⚡ Neden E-Helper? (Temel Avantajlar)

1. **Casper Excalibur'a Özel:** Sadece Casper Excalibur dizüstü bilgisayar donanımına odaklanmıştır; başka cihaz ve modellerin karmaşıklığını barındırmaz.
2. **Ultra Düşük Bellek Tüketimi:** Arka planda sistem tepsisinde beklerken yalnızca **~1 - 5 MB RAM** harcar. Yönetim paneli ekranda aktif açıkken dahi tüketim **~40 - 70 MB** aralığında seyreder.
3. **Sıfır Arka Plan Yükü (Bloat-Free):** Arka planda çalışan servisler (daemons), telemetri takipçileri, analitik ajanları ve CPU'yu meşgul eden gereksiz süreçler yoktur.
4. **Kalıcı Ayar Korunumu:** Yapılan ayarlar (klavye RGB aydınlatması, fan/güç modu, parlaklık) bilgisayar kapatılıp açıldığında dahi kaybolmaz; Windows Görev Zamanlayıcısı başlangıç entegrasyonu ve açılış sonrası EC firmware senkronizasyonu ile otomatik korunur.
5. **Doğrudan Donanım Köprüsü:** Üreticinin hantal servisleri yerine doğrudan anakart gömülü denetleyicisiyle (EC / WMI ACPI SMI) güvenli ve yerel iletişim kurar.
6. **Kompakt Sistem Tepsisi Paneli:** Görev çubuğunda yer kaplamaz, bildirim alanındaki simgesine tıklandığında ekranın köşesinde fırlayan modern bir arayüz açılır ve istendiğinde ekranda serbestçe taşınabilir.
7. **Çift Dil & Çift Tema:** Tek tıkla Türkçe ve İngilizce, ayrıca G-Helper esintili Mat Koyu ve Ferah Açık tema desteği.

---

## 🎯 Özellikler

### 1. Performans Modları
* **Tasarruf (Eco / Office):** Düşük fan devri, sessiz çalışma ortamı ve güç tasarrufu.
* **Dengeli (Balanced / Gaming):** Dinamik fan eğrisi ve dengeli termal yönetim.
* **Performans (Performance / Turbo):** Maksimum fan devri ve en yüksek donanım gücü.
* *Windows yerel güç planlarıyla (`PowerSetActiveScheme`) otomatik senkronize çalışır.*

### 2. Canlı Donanım Telemetrisi
* **CPU & GPU Değerleri:** Anlık işlemci ve ekran kartı sıcaklıkları (°C) ile fan RPM değerleri.
* **Sistem Kaynakları:** Canlı RAM kullanımı (GB ve yüzde) ve sistem diski SSD (C:) doluluk oranı.
* **Sistem Tepsisi İpucu (Tooltip):** Fare tepsi simgesinin üzerine getirildiğinde pencereyi açmadan anlık sıcaklık ve RPM bilgileri görüntülenir.

### 3. RGB Klavye Aydınlatma Denetimi
* **Modlar:** Sabit (Static), Nefes Alma (Breathing), Renk Döngüsü (Dinamik), Gökkuşağı (Rainbow) ve **3-Bölge Özel Klavye Aydınlatması**.
* **İnteraktif 3-Bölge Klavye Haritası:** Kuşbakışı 2D 3 parçalı klavye sembolü ile Sol (WASD), Orta ve Sağ (Numpad) alanlarına bağımsız renk tanımlayabilme.
* **Modern Renk Paleti:** Windows 11 tarzı 2D renk alanı, 1D ton kaydırıcısı, Hex kodu girişi, RGB değerleri ve hazır renk kartelasıyla zengin renk seçimi.
* **Donanımsal Parlaklık Kademeleri (Dolgulu Gösterge):**
  * `0` : **Kapalı (%0)** — Tüm klavye ışıklarını donanımsal olarak kapatır.
  * `1` : **%50 Parlaklık** — Dengeli ortam parlaklığı.
  * `2` : **%100 Parlaklık** — Tam parlaklık seviyesi.
  * *Kaydırıcı çubuğun istenen noktasına tıklanarak doğrudan ayarlanabilir.*
* **Yeniden Başlatmada Korunum:** Bilgisayar yeniden başlatıldığında klavye aydınlatma modunuz, parlaklığınız ve bölge renkleriniz otomatik olarak tekrar uygulanır.

### 4. Kullanılabilirlik & Sistem Entegrasyonu
* **Başlangıçta Çalıştır:** Windows açılışında UAC uyarısı vermeden arka planda sessizce başlatılan Görev Zamanlayıcısı entegrasyonu.
* **Tek Tıkla Tema Değişimi:** Mat Koyu ve Ferah Açık tema seçenekleri.
* **Entegre Güncelleyici:** GitHub üzerinden yeni sürümleri tek tıkla sorgulama.

---

## 💻 Donanım Desteği

E-Helper sadece **Casper Excalibur** ailesini destekler:

### ✅ Casper Excalibur Ailesi (Doğrudan ACPI SMI Motoru)
> Fiziksel donanım üzerinde test edilmiş ve sorunsuz çalıştığı doğrulanmıştır: **Excalibur G770**, **G850**, **G870** ve **G911** (performans profilleri, telemetri ve klavye RGB aydınlatması).

* **Excalibur G770** (Test Edildi & Doğrulandı)
* **Excalibur G850 / G860** (Test Edildi & Doğrulandı)
* **Excalibur G870** (Test Edildi & Doğrulandı)
* **Excalibur G911** (Test Edildi & Doğrulandı)
* **Excalibur G780**
* **Excalibur G900**
* **Excalibur G920**

---

## 🚀 İndirme ve Kullanım

1. [Releases (Sürümler)](https://github.com/berkevnl/E-Helper/releases/latest) sayfasına gidin.
2. `E-Helper.exe` dosyasını indirin (kurulum gerekmez).
3. `E-Helper.exe` dosyasına sağ tıklayıp **Yönetici Olarak Çalıştır** deyin *(Windows'un ACPI WMI SMI donanım kontrolcüsüne erişebilmesi için yönetici yetkisi gereklidir)*.
4. E-Helper ekranın sağ altındaki **Sistem Tepsisine** (saatin yanına) yerleşecektir. Simgeye tıklayarak paneli açıp kapatabilirsiniz!

---

## 💖 Teşekkür & İlham Kaynağı

Bu proje, **[Seerge](https://github.com/seerge)** tarafından geliştirilen öncü **[G-Helper](https://github.com/seerge/g-helper)** projesinden ilham almıştır.

---

## ⚖️ Yasal Haklar Bildirimi

* Bu projede adı geçen marka ve model isimleri yalnızca donanım uyumluluğunu belirtmek amacıyla kullanılmıştır ve ilgili hak sahiplerinin mülkiyetindedir.
* **E-Helper**, bağımsız ve açık kaynaklı bir topluluk projesidir.
* Bu yazılım **GNU General Public License v3.0 (GPL-3.0)** lisansı altında, hiçbir garanti verilmeksizin "olduğu gibi" sunulmaktadır.
