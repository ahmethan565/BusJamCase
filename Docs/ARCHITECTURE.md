# Bus Jam Benzeri Bulmaca Oyunu — Mimari Doküman

> **Bu doküman kimin için?** Kodu yazacak geliştiriciler ve Claude Code session'ları için tek referans kaynağı.
> Buradaki her karar ekipte tartışılarak alındı. "Neden böyle?" sorusunun cevabı ilgili bölümde ve **§19 Karar Kaydı**'nda.
> Bir kararı değiştirmek gerekirse önce bu doküman güncellenir, sonra kod.

---

## İçindekiler

1. [Hedefler ve Kısıtlar](#1-hedefler-ve-kısıtlar)
2. [Oyun Kuralları (Kesin Tanım)](#2-oyun-kuralları-kesin-tanım)
3. [Mimariye Genel Bakış](#3-mimariye-genel-bakış)
4. [Proje Kurulumu](#4-proje-kurulumu)
5. [Klasör ve Assembly Yapısı](#5-klasör-ve-assembly-yapısı)
6. [Core Katmanı](#6-core-katmanı)
7. [Bölüm Verisi, Doğrulama ve Çözücü](#7-bölüm-verisi-doğrulama-ve-çözücü)
8. [İlerleme Kaydı](#8-ilerleme-kaydı)
9. [Dünya Yerleşimi, Kamera ve Input](#9-dünya-yerleşimi-kamera-ve-input)
10. [View Katmanı ve Animasyonlar](#10-view-katmanı-ve-animasyonlar)
11. [Uygulama Akışı (Durum Makinesi)](#11-uygulama-akışı-durum-makinesi)
12. [UI Katmanı](#12-ui-katmanı)
13. [Ses ve Efektler](#13-ses-ve-efektler)
14. [Composition Root ve Sahne Hiyerarşisi](#14-composition-root-ve-sahne-hiyerarşisi)
15. [Editör Araçları](#15-editör-araçları)
16. [Performans](#16-performans)
17. [Test Stratejisi](#17-test-stratejisi)
18. [Uygulama Fazları ve Kabul Kriterleri](#18-uygulama-fazları-ve-kabul-kriterleri)
19. [Karar Kaydı](#19-karar-kaydı)
20. [Kapsam Dışı](#20-kapsam-dışı)
21. [Ekler: 5 Bölüm ve Referans Simülatör](#21-ekler-5-bölüm-ve-referans-simülatör)

---

## 1. Hedefler ve Kısıtlar

### Hedefler
- Rollic Games'in **Bus Jam** oyununun mekaniğini temel alan, **en az 5 bölüm** oynanabilir bir **3D mobil** bulmaca oyunu.
- **OOP ve SOLID** ilkelerine uyan, **temiz ve okunabilir** bir kod tabanı.
- **Matematik fevkalade basit olmalı:** ızgara koordinatları, BFS, `süre = mesafe / hız` ve DOTween'in isimlendirilmiş easing'leri dışında matematik yok.
- Mobilde akıcı performans (bkz. §16).

### Sabit kısıtlar
| Kısıt | Değer |
|---|---|
| Motor | **Unity 2022.3.19f1** (sürüm sabit, yükseltilmeyecek) |
| Dil | C# (Unity 2022.3'ün desteklediği C# 9 alt kümesi — bkz. §6.0) |
| Render | URP 14.x |
| Platform | Mobil (Android öncelikli, iOS uyumlu), **sadece dikey (portrait)** |
| Tween | **DOTween** (ücretsiz sürüm) |
| UI | **UGUI + TextMeshPro** |
| Input | **Input System paketi** |

### Okunabilirlik kuralları (her kod satırı için geçerli)
- Her sınıfın **tek bir sorumluluğu** var; adı bu sorumluluğu anlatır.
- **Aşırı soyutlama yok:** Arayüz sadece (a) değişebilecek, (b) testte sahtesi gereken, ya da (c) katman sınırını koruyan yerlerde. Tam liste §19'da.
- **MonoBehaviour içinde oyun kuralı yok.** Kurallar sadece `Game.Core`'da.
- **Singleton, Service Locator, `FindObjectOfType` yok.** Bağımlılıklar `GameBootstrap`'te elle bağlanır.
- Inspector'daki `OnClick` listeleri kullanılmaz; butonlar **kodda** bağlanır.
- "Sihirli sayı" yok: oyun hissini belirleyen her değer bir ScriptableObject ayarında.

### Kod dili kuralı
- Tanımlayıcılar (sınıf, metot, değişken adları) ve kod yorumları **İngilizce**.
- Oyuncunun gördüğü metinler ve editör araçlarının metinleri (bölüm editörü, katalog, doğrulama/parser hataları) **İngilizce**. Projede Türkçe metin yoktur; sadece bu doküman Türkçedir.
- *(Ekip tercihine göre değiştirilebilir; değiştirilirse bu satır güncellenmeli.)*

---

## 2. Oyun Kuralları (Kesin Tanım)

Bu bölüm, `GameSession`'ın davranışının **tek doğru tanımıdır.** Testler buna göre yazılır. Referans Python simülatörü (§21.2) bu bölümdeki oynanış kurallarını birebir uygular.

### 2.1 Varlıklar
- **Tahta:** `Width × Height` kare hücre. Her hücre `Floor`, `Wall` ya da `Void`.
- **Yolcu:** Bir `Floor` hücresinde durur, bir rengi var.
- **Otobüs sırası:** Sıralı otobüs listesi. Her otobüsün bir rengi ve kapasitesi var (varsayılan 3). Kapıda her an en fazla **bir aktif otobüs** bulunur.
- **Bekleme alanı:** Sabit sayıda koltuk (varsayılan 5, bölüm başına ayarlanabilir).

### 2.2 Koordinatlar
- `x`: sütun, soldan sağa (0'dan başlar).
- `y`: satır, **yukarıdan aşağıya** (0'dan başlar). `y = 0` yola en yakın satırdır.
- Tahtanın dışı `Void` kabul edilir.

### 2.3 Hücre tipleri
| Tip | Karakter | Yürünebilir mi? | Var mı? (çıkış hesabı için) | Görsel |
|---|---|---|---|---|
| `Floor` | `.` veya renk harfi | Üzerinde yolcu yoksa evet | Evet | Zemin karosu |
| `Wall` | `#` | Hayır | Evet | Engel objesi |
| `Void` | `-` | Hayır | **Hayır** | Hiçbir şey çizilmez |

### 2.4 Çıkış hücresi
> **Bir `Floor` hücresi, kendi sütununda üstündeki hücrelerin hepsi `Void` ise çıkış hücresidir.** (`y = 0`'daki her `Floor` hücresi bu tanımla çıkıştır.)

- Duvarın altındaki hücre çıkış **değildir** (duvar "var olan" bir hücredir).
- Void'in altındaki hücre çıkış **olabilir.** Bu sayede üçgen ve düzensiz tahtalar ile tahtanın üst kenarındaki girintiler doğal olarak çalışır.
- Tahtanın ortasındaki bir void deliğin altındaki hücre, deliğin üstünde var olan bir hücre olduğu için çıkış **değildir.**

### 2.5 Yol
Bir yolcunun **yolu var** demek: yolcunun hücresinden başlayıp, sadece **boş `Floor`** hücrelerden 4 yönlü (yukarı/aşağı/sol/sağ) geçerek bir çıkış hücresine ulaşılabilir. Yolcu zaten çıkış hücresindeyse yolu var demektir (yol = sadece kendi hücresi).

### 2.6 Dokunuş (tek hamle türü)
Oyuncu bir hücreye dokunur:
1. Oyun bitmişse (kazanıldı/kaybedildi) → **hiçbir şey olmaz.**
2. Hücrede yolcu yoksa → **hiçbir şey olmaz.**
3. Yolcunun yolu yoksa → `OnPassengerBlocked` olayı yayınlanır, **durum değişmez.**
4. Yolu varsa yolcu **anında** tahtadan kalkar (`OnPassengerExited`). Sonra:
   - a. Aktif otobüs yolcunun renginde ve dolu değilse → **otobüse biner** (§2.7).
   - b. Değilse → **en küçük numaralı boş koltuğa** oturur (`OnPassengerWaiting`). Bu oturuşla bekleme alanı **dolduysa** → **kaybedilir** (`OnLost`).
   - (Oyun sürerken bekleme alanı hiçbir zaman dolu kalmaz; bu yüzden b adımında her zaman boş koltuk vardır.)
5. Tahtada ve bekleme alanında hiç yolcu kalmadıysa → **kazanılır** (`OnWon`). Bu kontrol **dokunuşun en sonunda, tek bir yerde** yapılır.

### 2.7 Biniş ve otobüs zinciri
Yolcu otobüse bindiğinde:
1. Otobüse eklenir (`OnPassengerBoarded`, koltuk numarasıyla).
2. Otobüs dolduysa: gider (`OnBusDeparted`), sıradaki otobüs gelir (`OnBusArrived`, sıra bittiyse gelmez).
3. Yeni gelen otobüsün renginde bekleme alanında yolcu varsa, **en küçük koltuk numarasından başlayarak** sırayla binerler (`OnPassengerLeftWaitingArea` + `OnPassengerBoarded`). Bu biniş de otobüsü doldurabilir; zincir kendiliğinden devam eder.

Bekleme alanında koltuktan kalkan yolcunun yeri boş kalır; **diğer yolcular kaymaz.**

### 2.8 Kaybetme koşulu (kesin)
Kaybetme **sadece** §2.6 adım 4b'de olur: bekleme alanının **son boş koltuğu doldurulduğu anda** oyun kaybedilir.
- Koltuklar dolduktan sonra oyuncunun başka hamlesi yoktur; aktif otobüsün renginde yolcu olsa bile oyun bitmiştir.
- Dolayısıyla N koltuklu bir bölümde oyuncu aynı anda en fazla N-1 yolcuyu bekletebilir.
- Kayıp anında yolcu tahtadan kalkmış ve son koltuğa yerleşmiş olur: olay sırası `OnPassengerExited` → `OnPassengerWaiting` → `OnLost` (görsel karşılığı §10.5'te).

### 2.9 Kilitlenme (softlock) olmaz
Bölüm doğrulamayı (§7.3) geçtiyse:
- Tahta boş değilse **her zaman en az bir yolcunun yolu vardır.** (Çıkıştan geriye doğru boş hücrelerle ilerleyen bölge ya bir yolcuya değer ya da yolcu zaten çıkıştadır.)
- Renk sayıları otobüslerle eşleştiği için, son yolcu bindiğinde son otobüs tam dolar.

Referans simülatörde 5 bölümün her biri için 3000 rastgele oyunda **0 kilitlenme** ölçüldü.

### 2.10 Hızlı dokunma
- Oyuncu animasyonları beklemeden **art arda dokunabilir.** Mantık her dokunuşu anında sonuçlandırır.
- Örnek: Arka arkaya duran A (önde) ve B'ye (arkada) hızlıca dokunulursa, A'nın hücresi mantıkta anında boşaldığı için B'nin de yolu vardır; ikisi de çıkar.
- Input sadece popup'lar açıkken kapalıdır (§11).

### 2.11 Süre (Faz 9)
Ayrıntı ve gerekçeler: `Docs/TIMER_DESIGN.md`.
- Her bölümün bir süresi var (`TimeLimitSeconds`); `0` = zamansız.
- Süre bölüm açıldığında durur, **tahtaya ilk dokunuşta** başlar (boş hücre, tahta dışı ya da yolu kapalı yolcu da sayılır; UI'ya dokunmak sayılmaz).
- Süre sıfıra inerse bölüm kaybedilir (`LoseReason.TimeUp`); koltuklar dolunca kaybetme `LoseReason.NoSpace` olur.
- Bölüm mantıkta kazanıldığı/kaybedildiği anda süre durur. Süre Core'da yaşar; Core saati okumaz, geçen süre dışarıdan `GameSession.Tick(deltaSeconds)` ile verilir.

---

## 3. Mimariye Genel Bakış

### 3.1 Katmanlar

```
┌──────────────────────────────────────────────────────────────┐
│  Game.Editor   (sadece editör: bölüm editörü, debug menüsü)    │
└──────────────┬───────────────────────────────────────────────┘
               │ referans verir
┌──────────────▼───────────────────────────────────────────────┐
│  Game.Runtime  (Unity tarafı)                                  │
│   Flow (durumlar) · View (3D) · Animation · UI · Input ·       │
│   Feedback (ses/efekt) · Data (ScriptableObject'ler) · Bootstrap│
└──────────────┬───────────────────────────────────────────────┘
               │ referans verir           ▲ IGameListener olayları
┌──────────────▼───────────────────────────┴───────────────────┐
│  Game.Core     (saf C#, UnityEngine referansı YOK)             │
│   Kurallar · Tahta · Yol bulma · Bölüm verisi/doğrulama/çözücü │
│   · İlerleme mantığı                                           │
└──────────────────────────────────────────────────────────────┘
```

- **Bağımlılık yönü tek:** Editor → Runtime → Core. Core hiçbir şeye bağlı değil.
- `Game.Core` asmdef'inde **"No Engine References" işaretli.** Core'a `using UnityEngine` yazılırsa proje derlenmez. Katman kuralı iyi niyete değil derleyiciye emanet.

### 3.2 Temel ilke: mantık anında, görsel sonra
- `GameSession` bir dokunuşu **milisaniyede** tamamen sonuçlandırır ve olay yayınlar.
- `GameplayPresenter` (View) bu olayları alır, animasyonlara çevirir. **Mantık asla animasyonu beklemez.**
- Kazanma/kaybetme popup'ı, mantıkta oyun bittikten sonra **tüm animasyonlar bitince** açılır.

### 3.3 Bir dokunuşun yolculuğu

```
Parmak ──▶ InputHandler ──CellTapped(GridPos)──▶ GameSession.Tap()
                                                      │ (anında, kurallar)
                                                      ▼
                                IGameListener olayları (senkron, sıralı)
                                                      │
                                                      ▼
                                            GameplayPresenter
                                   ┌──────────────────┴──────────────────┐
                           serbest animasyonlar                    BusLane (sıralı)
                       (yürüyüş, squash, sallanma)        (biniş, otobüs gidiş/geliş)
                                   └──────────────────┬──────────────────┘
                                                      ▼
                                       ses/efekt (IFeedback), animasyon anında
```

### 3.4 Arayüzlerin tam listesi (başka arayüz eklenmez)
| Arayüz | Katman | Neden var? |
|---|---|---|
| `IGameListener` | Core | Core'un View'a bağımlı olmaması (D ilkesi); testte kayıt tutan sahte listener |
| `IPathFinder` | Core | Yol bulma algoritması değiştirilebilir; çözücü ve doğrulayıcı da kullanır |
| `ILevelCatalog` | Core | Bölüm kaynağı değiştirilebilir (bugün ScriptableObject, yarın uzak JSON); testte sahte katalog |
| `IProgress` | Core | Akış durumları kayıt detayını bilmez; testte sahte |
| `IProgressStorage` | Core | Depolama değiştirilebilir (PlayerPrefs → dosya → bulut); testte bellek içi |
| `IAppState` | Runtime | Durum makinesinin sözleşmesi |
| `IStep` | Runtime | `BusLane` adımlarının sözleşmesi |
| `IFeedback` | Runtime | Animasyon kodu ses/efekt detayını bilmez |

`Bus`, `WaitingArea`, `Board`, `BusQueue` gibi sınıflar **somut** kalır.

---

## 4. Proje Kurulumu

### 4.1 Paketler
| Paket | Kaynak | Not |
|---|---|---|
| Universal RP | Package Manager | 2022.3 için doğrulanmış (verified) 14.x sürümü |
| Input System | Package Manager | 2022.3 için doğrulanmış 1.7.x sürümü |
| TextMeshPro | Package Manager | 2022.3'te **ayrı paket** (3.0.x). İlk açılışta **TMP Essential Resources** içe aktarılır |
| Test Framework | Package Manager | Varsayılan olarak gelir (1.1.x) |
| DOTween (ücretsiz) | Asset Store | Kurulumdan sonra **DOTween Utility Panel → "Create ASMDEF"** butonuna basılmalı. Basılmazsa `Game.Runtime` DOTween'i göremez |

### 4.2 Player Settings
| Ayar | Değer |
|---|---|
| Active Input Handling | **Input System Package (New)** |
| Default Orientation | **Portrait** (Landscape seçenekleri kapalı) |
| Color Space | Linear |
| Scripting Backend (Android) | IL2CPP |
| Target Architectures (Android) | ARM64 (Google Play zorunluluğu) |
| Texture Compression (Android) | ASTC |

### 4.3 Project Settings
| Ayar | Değer |
|---|---|
| Audio → DSP Buffer Size | **Best Latency** (dokunuş-ses gecikmesini azaltır) |
| Quality | Mobil için tek seviye; URP asset'i §16.2'deki ayarlarla |
| Editor → Enter Play Mode Options | **Kapalı bırakılır** (varsayılan). Açılırsa (domain reload kapalı) ScriptableObject'lerdeki çalışma zamanı alanları Play modları arasında taşınabilir |

### 4.4 TextMeshPro font
Metinler İngilizce olduğu için ASCII yeterlidir. Tüm UI metinleri `Content/Fonts/Zany SDF` (Zany.ttf'den, dinamik atlas; ASCII karakterleri atlasa önceden gömülü) kullanır; TMP Settings'teki varsayılan font da budur. Yedek (fallback) font yok; eski `LiberationSans_TR SDF` kullanılmadığı için silindi. Kabul kriteri: sahnedeki her metnin tüm karakterleri font'ta var (kare/boşluk yok); bunu `UiWiringTests` denetler.

---

## 5. Klasör ve Assembly Yapısı

### 5.1 Klasörler
```
(Unity proje kökü)
CLAUDE.md                         Claude Code kuralları
Docs/                             Assets dışında; Unity içe aktarmaz
  ARCHITECTURE.md                 bu doküman
  reference/                      referans simülatör
    bus_jam_sim.py
    levels.py
Assets/
  _Game/
    Core/                         → Game.Core.asmdef (No Engine References ✔)
      GridPos.cs
      CellType.cs
      PassengerColor.cs
      Passenger.cs
      Board.cs
      Bus.cs
      BusQueue.cs
      WaitingArea.cs
      GameSession.cs
      SessionResult.cs
      IGameListener.cs
      NullGameListener.cs
      Path/
        IPathFinder.cs
        BfsPathFinder.cs
      Levels/
        LevelData.cs
        PassengerSpawn.cs
        LevelParser.cs
        ParseResult.cs
        LevelValidator.cs
        LevelSolver.cs                (çözücü + EstimateRandomWinRate)
        SolveResult.cs
        ILevelCatalog.cs
      Progress/
        SaveData.cs
        IProgress.cs
        IProgressStorage.cs
        ProgressService.cs
        InMemoryProgressStorage.cs    (testler, Faz 4 ve editörden bölüm başlatma için)

    Runtime/                      → Game.Runtime.asmdef
      Bootstrap/
        GameBootstrap.cs
        EditorLevelRequest.cs         (sadece UNITY_EDITOR)
      Data/
        LevelAsset.cs
        LevelCatalog.cs
        ColorPalette.cs
        AnimationSettings.cs
        LayoutSettings.cs
        SoundLibrary.cs
        SoundId.cs
        VfxId.cs
      Flow/
        IAppState.cs
        AppStateMachine.cs
        AppStates.cs
        AppDependencies.cs            (AppStates'in bağımlılık paketi)
        MenuState.cs
        GameplayState.cs
        WinState.cs
        LoseState.cs
      View/
        BoardLayout.cs
        BoardView.cs
        PassengerView.cs
        BusView.cs
        WaitingAreaView.cs
        GameplayPresenter.cs
        StopSeats.cs                  (ekrandaki durak koltukları, §10.5)
        CameraFitter.cs
      Animation/
        IStep.cs
        BusLane.cs
        BusArriveStep.cs
        BoardStep.cs
        BusDepartStep.cs
      Input/
        InputHandler.cs
        BackButtonInput.cs
      UI/
        ScreenView.cs
        LevelMapScreen.cs
        LevelNodeView.cs
        Hud.cs
        WinScreen.cs
        LoseScreen.cs
        QuitConfirmScreen.cs
        SafeAreaFitter.cs
        UiText.cs
      Feedback/
        IFeedback.cs
        FeedbackPlayer.cs
        NullFeedback.cs               (Faz 3–5 için sessiz uygulama)
        PooledParticle.cs             (parçacık bitince havuza dönüş)
        AudioPlayer.cs
        VfxPlayer.cs
      Progress/
        PlayerPrefsProgressStorage.cs

    Editor/                       → Game.Editor.asmdef (sadece Editor platformu)
      LevelAssetEditor.cs
      LevelCatalogEditor.cs
      DebugMenu.cs
      EditorLevelLauncher.cs
      LevelEditing.cs                 (bölüm editörünün test edilebilir mantığı)

    Tests/
      Core/                       → Game.Core.Tests.asmdef (EditMode)
      Editor/                     → Game.Editor.Tests.asmdef (EditMode, katalog testleri)

    Content/
      Levels/                     LevelAsset'ler ve LevelCatalog
      Settings/                   AnimationSettings, LayoutSettings, ColorPalette, SoundLibrary
      Prefabs/
      Materials/
      Audio/
      Fonts/
    Scenes/
      Main.unity                  TEK sahne
```

### 5.2 Assembly referansları
| Assembly | Referanslar | Platform |
|---|---|---|
| `Game.Core` | — (hiçbiri) | Hepsi, **No Engine References** |
| `Game.Runtime` | `Game.Core`, `UnityEngine.UI` (UGUI 2022.3'te paket), `Unity.InputSystem`, `Unity.TextMeshPro`, `DOTween.Modules` (Create ASMDEF ile oluşan) | Hepsi |
| `Game.Editor` | `Game.Core`, `Game.Runtime` | Sadece Editor |
| `Game.Core.Tests` | `Game.Core`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, NUnit ("Override References" → `nunit.framework.dll`) | Sadece Editor |
| `Game.Editor.Tests` | `Game.Core`, `Game.Runtime`, `Game.Editor`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, NUnit | Sadece Editor |

### 5.3 Namespace'ler
Klasörle birebir: `Game.Core`, `Game.Core.Path`, `Game.Core.Levels`, `Game.Core.Progress`, `Game.Runtime.Flow`, `Game.Runtime.View`, `Game.Runtime.UI` vb.

---

## 6. Core Katmanı

### 6.0 C# kuralları (Unity 2022.3)
- `record` ve `init` erişimcisi **kullanılmaz** (2022.3'te ek hile gerektirir).
- Değer nesneleri `readonly struct`, değişmez veriler `readonly` alanlı ve constructor ile kurulan sınıflar.
- Switch ifadeleri, pattern matching, `new()` serbest.
- Core'da `System.Linq` **oyun döngüsünde kullanılmaz** (doğrulayıcı/çözücü/test gibi oyun dışı kodda serbest).

### 6.1 `GridPos`
```csharp
public readonly struct GridPos : IEquatable<GridPos>
{
    public readonly int X;
    public readonly int Y;

    public GridPos(int x, int y) { X = x; Y = y; }

    public static readonly GridPos Up    = new GridPos(0, -1);   // yola doğru
    public static readonly GridPos Down  = new GridPos(0, 1);
    public static readonly GridPos Left  = new GridPos(-1, 0);
    public static readonly GridPos Right = new GridPos(1, 0);

    // BFS komşu sırası: önce yukarı (doğal görünen yollar için)
    public static readonly GridPos[] Directions = { Up, Left, Right, Down };

    public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.X + b.X, a.Y + b.Y);
    // Equals, GetHashCode, ==, !=, ToString() => $"({X},{Y})"
}
```

### 6.2 `CellType`, `PassengerColor`
```csharp
public enum CellType { Floor, Wall, Void }
public enum PassengerColor { Red, Green, Blue, Yellow, Purple, Orange }
```

### 6.3 `Passenger` (değişmez)
```csharp
public sealed class Passenger
{
    public int Id { get; }                  // bölüm içinde benzersiz; View eşlemesi için
    public PassengerColor Color { get; }
    public GridPos StartPosition { get; }   // View'ın ilk yerleşimi için
}
```
Yolcunun **güncel konumu** `Board`'da tutulur (tek bilgi kaynağı). Yolcu tahtadan kalkınca konumu artık bir hücre değildir.

### 6.4 `Board`
**Sorumluluğu:** Hangi hücrenin tipi ne, hangi hücrede hangi yolcu var. Kural bilmez.

```csharp
public sealed class Board
{
    public Board(int width, int height, CellType[] cells, IEnumerable<Passenger> passengers);

    public int Width { get; }
    public int Height { get; }
    public int PassengerCount { get; }
    public bool IsEmpty => PassengerCount == 0;
    public IReadOnlyList<GridPos> ExitCells { get; }   // constructor'da bir kez hesaplanır

    public bool InBounds(GridPos p);
    public CellType GetCell(GridPos p);         // tahta dışı → Void
    public bool Exists(GridPos p);              // GetCell(p) != Void
    public bool IsWalkable(GridPos p);          // Floor ve üzerinde yolcu yok
    public bool IsExit(GridPos p);              // §2.4
    public bool TryGetPassenger(GridPos p, out Passenger passenger);
    public void RemovePassenger(GridPos p);

    public Board Clone();                       // çözücü için derin kopya
}
```

**Veri yapısı:** Hücreler ve yolcular **düz dizilerde** tutulur:
```csharp
private readonly CellType[] cells;      // uzunluk Width * Height
private readonly Passenger[] occupants; // null = boş
private int Index(GridPos p) => p.Y * Width + p.X;
```

**Çıkış hesabı (§2.4):**
```csharp
private bool ComputeIsExit(GridPos p)
{
    if (GetCell(p) != CellType.Floor) return false;
    for (int y = p.Y - 1; y >= 0; y--)
        if (Exists(new GridPos(p.X, y))) return false;
    return true;
}
```

### 6.5 `Bus`
```csharp
public sealed class Bus
{
    public int Id { get; }                 // sıradaki index; View eşlemesi için
    public PassengerColor Color { get; }
    public int Capacity { get; }
    public int Count { get; private set; }
    public bool IsFull => Count == Capacity;

    public bool CanBoard(Passenger p) => !IsFull && p.Color == Color;
    public int Add(Passenger p);           // koltuk index'ini döner (0..Capacity-1); CanBoard false ise InvalidOperationException
    public Bus Clone();
}
```

### 6.6 `BusQueue`
```csharp
public sealed class BusQueue
{
    public BusQueue(IEnumerable<Bus> buses);
    public Bus Current { get; private set; }   // sıra bittiyse null
    public int RemainingCount { get; }         // Current dahil (hesaplanan özellik)
    public void Advance();                 // Current'ı bir sonrakine geçirir
    public BusQueue Clone();
}
```

### 6.7 `WaitingArea`
```csharp
public sealed class WaitingArea
{
    public WaitingArea(int slotCount);
    public int SlotCount { get; }
    public bool IsFull { get; }
    public bool IsEmpty { get; }
    public Passenger GetAt(int slot);                       // boşsa null

    // En küçük numaralı boş koltuğa koyar.
    public bool TryAdd(Passenger p, out int slot);

    // Verilen renkteki, en küçük numaralı koltuktaki yolcuyu çıkarır. Diğerleri KAYMAZ.
    public bool TryTake(PassengerColor color, out Passenger p, out int slot);

    public WaitingArea Clone();
}
```

### 6.8 `IGameListener` (Core → dış dünya)
```csharp
public interface IGameListener
{
    void OnBusArrived(Bus bus);
    void OnPassengerBlocked(Passenger passenger);
    void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path);
    void OnPassengerWaiting(Passenger passenger, int slot);
    void OnPassengerLeftWaitingArea(Passenger passenger, int slot);
    void OnPassengerBoarded(Passenger passenger, Bus bus, int seat);
    void OnBusDeparted(Bus bus);
    void OnWon();
    void OnLost();
}
```
- Olaylar **senkron** ve **mantıktaki gerçekleşme sırasıyla** yayınlanır.
- `path`, yolcunun başlangıç hücresinden çıkış hücresine kadar (ikisi dahil) hücre listesidir. Her başarılı dokunuşta yeni bir liste oluşturulur; dinleyici saklayabilir.
- `NullGameListener`: hiçbir şey yapmayan uygulama (çözücü ve bazı testler için).

**Örnek olay sırası** — otobüste 1 boş koltuk var, kırmızı yolcuya dokunuluyor, sonraki otobüs mavi, bekleme alanında 1 mavi var:
```
OnPassengerExited(red, path)
OnPassengerBoarded(red, bus#0, seat 2)
OnBusDeparted(bus#0)
OnBusArrived(bus#1)
OnPassengerLeftWaitingArea(blue, slot 0)
OnPassengerBoarded(blue, bus#1, seat 0)
```

### 6.9 `GameSession` (kuralların orkestra şefi)
```csharp
public enum SessionResult { Playing, Won, Lost }

public sealed class GameSession
{
    public GameSession(LevelData level, IPathFinder pathFinder, IGameListener listener);

    public SessionResult Result { get; private set; }
    public bool IsOver => Result != SessionResult.Playing;
    public Board Board { get; }              // testler için okuma amaçlı
    public BusQueue Buses { get; }
    public WaitingArea WaitingArea { get; }

    public void Start();                     // started = true; ilk otobüs için OnBusArrived yayınlar; ikinci çağrı InvalidOperationException
    public void Tap(GridPos cell);           // Start'tan önce çağrılırsa InvalidOperationException (programlama hatası)

    internal GameSession Clone(IGameListener listener);   // sadece LevelSolver için
    internal string StateKey { get; }                      // sadece LevelSolver için (ziyaret kümesi)
}
```

**Referans uygulama (okunabilirlik hedefi bu seviyede):**
```csharp
public void Tap(GridPos cell)
{
    if (!started) throw new InvalidOperationException("Call Start() before Tap().");
    if (IsOver) return;
    if (!board.TryGetPassenger(cell, out Passenger passenger)) return;

    if (!pathFinder.TryFindPathToExit(board, cell, out List<GridPos> path))
    {
        listener.OnPassengerBlocked(passenger);
        return;
    }

    board.RemovePassenger(cell);
    listener.OnPassengerExited(passenger, path);

    if (buses.Current != null && buses.Current.CanBoard(passenger))
        BoardPassenger(passenger);
    else
    {
        // The area is never left full while playing (§2.8), so a free slot always exists here.
        waitingArea.TryAdd(passenger, out int slot);
        listener.OnPassengerWaiting(passenger, slot);
        if (waitingArea.IsFull)
        {
            Finish(SessionResult.Lost);
            return;
        }
    }

    if (board.IsEmpty && waitingArea.IsEmpty)
        Finish(SessionResult.Won);
}

private void BoardPassenger(Passenger passenger)
{
    Bus bus = buses.Current;
    int seat = bus.Add(passenger);
    listener.OnPassengerBoarded(passenger, bus, seat);

    if (bus.IsFull)
        ReplaceFullBus(bus);
}

private void ReplaceFullBus(Bus fullBus)
{
    listener.OnBusDeparted(fullBus);
    buses.Advance();
    if (buses.Current == null) return;

    listener.OnBusArrived(buses.Current);
    BoardWaitingPassengers();
}

private void BoardWaitingPassengers()
{
    // BoardPassenger() otobüsü doldurursa ReplaceFullBus → BoardWaitingPassengers yeniden çalışır;
    // bu döngü her turda güncel Current'ı yeniden okur.
    while (buses.Current != null
           && waitingArea.TryTake(buses.Current.Color, out Passenger waiting, out int slot))
    {
        listener.OnPassengerLeftWaitingArea(waiting, slot);
        BoardPassenger(waiting);
    }
}

private void Finish(SessionResult result)
{
    Result = result;
    if (result == SessionResult.Won) listener.OnWon();
    else listener.OnLost();
}
```
> Not: `BoardPassenger()` otobüsü doldurursa `ReplaceFullBus` hemen sıradaki otobüse geçer. Bu yüzden dolu bir otobüs hiçbir zaman `Current` olarak kalmaz ve döngüde ek bir `IsFull` kontrolü gerekmez. Metodun adı `BoardPassenger`'dır çünkü `Board` adı tahta özelliği için kullanılıyor.

### 6.10 `IPathFinder` / `BfsPathFinder`
```csharp
public interface IPathFinder
{
    // Yol varsa başlangıç→çıkış (ikisi dahil) yeni bir liste döner.
    bool TryFindPathToExit(Board board, GridPos from, out List<GridPos> path);

    // Tüm çıkış hücrelerinden geriye doğru erişilebilen Floor hücreleri.
    // ignorePassengers = true: yolcular engel sayılmaz (bölüm doğrulaması için).
    HashSet<GridPos> ReachableFromExits(Board board, bool ignorePassengers);
}
```
**`BfsPathFinder` uygulama kuralları:**
- Komşu sırası `GridPos.Directions` (Up, Left, Right, Down).
- Başlangıç hücresi çıkışsa yol `[from]`.
- Geçilebilir hücre: `board.IsWalkable(n)` (başlangıç hücresi hariç; o yolcunun kendi hücresi).
- İlk ulaşılan çıkış hücresinde durur; `parent` dizisinden yol geri kurulur ve ters çevrilir.
- **Tamponlar yeniden kullanılır:** `visited` (`bool[]`), `parent` (`int[]`), kuyruk (`int[]`) — boyut `Width * Height`; tahta boyutu değişirse yeniden ayrılır. Her aramada sadece sonuç listesi yeni oluşturulur.
- `ReachableFromExits`: çok başlangıçlı BFS; tüm çıkış hücreleri kuyruğa başlangıç olarak konur. Oyun dışı kullanım (doğrulama), tahsis serbest.

---

## 7. Bölüm Verisi, Doğrulama ve Çözücü

### 7.1 Metin formatı
Bir bölüm, satır satır bir metin ızgarası ve birkaç sayıdan oluşur:

```
Rows:          "RRBBGG"
               "Y-RG-B"
               "GYB..R"
               "-BR#G-"
               "-GYRB-"
Buses:         "GRBYGRB"      (soldan sağa geliş sırası; boşluklar yok sayılır)
BusCapacity:   3
WaitingSlots:  5
Version:       1
```

**Karakter tablosu** (`LevelParser` içinde tek bir sözlük; yeni hücre tipi = tabloya bir satır):
| Karakter | Anlam |
|---|---|
| `.` | Boş zemin (`Floor`) |
| `#` | Duvar (`Wall`) |
| `-` | Yok (`Void`) |
| `R` `G` `B` `Y` `P` `O` | Zemin + yolcu (Red, Green, Blue, Yellow, Purple, Orange) |

Otobüs dizisinde de aynı renk harfleri kullanılır.

### 7.2 `LevelData` (Core, değişmez)
```csharp
public sealed class LevelData
{
    public int Version { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<CellType> Cells { get; }              // Width * Height, index = y * Width + x
    public IReadOnlyList<PassengerSpawn> Passengers { get; }   // (Id, Color, Position)
    public IReadOnlyList<PassengerColor> BusColors { get; }
    public int BusCapacity { get; }
    public int WaitingSlots { get; }
}
```
- `LevelData` **değişmezdir.** Aynı `LevelData`'dan istenen sayıda yeni `GameSession` kurulabilir ("Tekrar dene" ve "Baştan başlat" bunu kullanır).
- Yolcu `Id`'leri ızgarada soldan sağa, yukarıdan aşağıya okuma sırasıyla 0'dan verilir. Otobüs `Id`'leri sıradaki index'tir.

### 7.3 `LevelParser` ve `LevelValidator`
```csharp
public static class LevelParser
{
    public static ParseResult Parse(IReadOnlyList<string> rows, string buses,
                                    int busCapacity, int waitingSlots, int version);
}
// ParseResult: bool Success, LevelData Level, IReadOnlyList<string> Errors

public sealed class LevelValidator
{
    public LevelValidator(IPathFinder pathFinder);
    public IReadOnlyList<string> Validate(LevelData level);   // boş liste = geçerli
}
```

**Parser hataları:** satır yok; satır uzunlukları farklı; tanımsız karakter; otobüs dizisinde tanımsız harf.

**Doğrulama kuralları (hepsi kontrol edilir, ilk hatada durulmaz):**
1. `Version` desteklenen sürüm (şu an sadece 1).
2. `BusCapacity ≥ 1`, `WaitingSlots ≥ 1`.
3. En az bir yolcu ve en az bir otobüs var.
4. **Her renk için:** `yolcu sayısı == o renkteki otobüs sayısı × BusCapacity`.
5. En az bir çıkış hücresi var.
6. **Erişilebilirlik:** `ReachableFromExits(board, ignorePassengers: true)` kümesinde olmayan her yolcu için hata ("(x,y) konumundaki yolcu çıkışa hiç ulaşamaz"). Void ve duvarlarla çevrili "adaları" yakalar.

Hata mesajları İngilizce ve konum içerir; bölüm editöründe olduğu gibi gösterilir.

### 7.4 `LevelSolver`
```csharp
public sealed class LevelSolver
{
    public LevelSolver(IPathFinder pathFinder);
    public SolveResult Solve(LevelData level, int maxStates = 200_000);

    // Rastgele oyuncu simülasyonu: kazanma oranı (0..1). Sadece editör ve testler kullanır.
    public float EstimateRandomWinRate(LevelData level, int games, int seed);
}
// SolveResult: bool IsSolvable, IReadOnlyList<GridPos> Solution, int StatesExplored, bool HitLimit
```
- **Algoritma:** Derinlik öncelikli arama (DFS) + ziyaret edilmiş durum kümesi.
- Her düğümde: o anda **yolu olan** yolcular denenir (yolu olmayana dokunmak durumu değiştirmez, denenmez).
- Kök session'da **`Start()` çağrılır**, sonra arama başlar (aynısı `EstimateRandomWinRate` için). `Clone`, `started` bayrağını da kopyalar.
- Durum kopyalama: `GameSession.Clone(NullGameListener)`; durum anahtarı: `GameSession.StateKey` (kalan yolcular + koltuklar + otobüs index'i + aktif otobüs doluluğu).
- `maxStates` aşılırsa `HitLimit = true` döner (sonuç "bilinmiyor" sayılır, test başarısız olur).
- Referans: §21.2'deki Python simülatörü aynı algoritmayı uygular; 5 bölümün her biri 1 saniyenin altında çözülür.
- **Zorluk tahmini (`EstimateRandomWinRate`, editör için):** `new System.Random(seed)` ile rastgele oyuncu simülasyonu — N oyunda her adımda yolu olan yolculardan rastgele biri seçilir, kazanma oranı raporlanır. Oran ne kadar düşükse bölüm o kadar zor. (Gerçek oyuncu planlı oynadığı için oran mutlak değil, **bölümler arası karşılaştırma** içindir.)

### 7.5 `ILevelCatalog` (Core) ve `LevelCatalog` (Runtime)
```csharp
public interface ILevelCatalog
{
    int Count { get; }
    LevelData Get(int index);
    bool HasNext(int index);       // index + 1 < Count
}
```
- `LevelCatalog : ScriptableObject, ILevelCatalog` — `List<LevelAsset>` tutar; **sıra = oyundaki bölüm sırası.**
- `Get` her çağrıda asset'i `LevelParser` ile çevirir (**önbellek yok**; parse ucuz ve bölüm başına bir kez çağrılır, editörde değiştirilen bölüm hemen yansır). Parse hatası varsa `Debug.LogError` + `InvalidOperationException` (bozuk bölüm oyuna girmemeli; zaten §17'deki katalog testi bunu yakalar).
- **"Son bölüm mü?" sorusunun tek cevabı `HasNext`'tir.** Kodda bölüm sayısı ("5") hiçbir yerde yazmaz.

> **Konuşmadan fark:** Tartışmada geçen `ILevelLoader`, `ILevelCatalog` ile birleştirildi. İkisi aynı soruyu (bölümler nereden geliyor?) cevaplıyordu. İleride uzaktan bölüm yüklemek = `RemoteJsonLevelCatalog : ILevelCatalog` yazmak.

### 7.6 `LevelAsset` (Runtime, ScriptableObject)
```csharp
[CreateAssetMenu(menuName = "Game/Level")]
public sealed class LevelAsset : ScriptableObject
{
    [SerializeField] private int version = 1;
    [SerializeField] private string[] rows;
    [SerializeField] private string buses;
    [SerializeField] private int busCapacity = 3;
    [SerializeField] private int waitingSlots = 5;

    public ParseResult Parse() => LevelParser.Parse(rows, buses, busCapacity, waitingSlots, version);
}
```
- Yapı bilinçli olarak **JSON'a birebir çevrilebilir** tutuldu (string dizisi + birkaç sayı).
- **Birincil veri kaynağı ScriptableObject'tir.** JSON / uzaktan yükleme şu an yok (§20).

### 7.7 Bölüm ekleme iş akışı
1. `Content/Levels` içinde: Create → Game → Level.
2. Bölüm editöründe (§15) ızgarayı boya, otobüs sırasını ve koltuk sayısını ayarla.
3. Canlı doğrulama hatalarını gider; **Çöz** butonuyla çözülebilir olduğunu gör.
4. **Bu bölümü oyna** ile dene.
5. `LevelCatalog`'un **sonuna** ekle.
6. Testleri çalıştır (§17.3 katalog testi tüm bölümleri doğrular ve çözer).

> **Yayın kuralı:** Oyun yayına çıktıktan sonra bölümler kataloğa **sadece sona eklenir.** Ortaya ekleme veya yeniden sıralama, oyuncuların kayıtlı ilerlemesini (index tabanlı) kaydırır. `LevelCatalogEditor` bu uyarıyı gösterir.

---

## 8. İlerleme Kaydı

### 8.1 Saklanan veri
```csharp
[Serializable]
public sealed class SaveData
{
    public int Version = 1;
    public int HighestCompletedIndex = -1;   // -1 = hiç bölüm geçilmedi
}
```
Zincir doğrusal olduğu için tek sayı yeterli. `Version`, ileride alan eklendiğinde eski kayıtları çevirmek için.

### 8.2 Kurallar ve depolama ayrı
```csharp
public interface IProgress
{
    bool IsUnlocked(int levelIndex);
    bool IsCompleted(int levelIndex);
    void MarkCompleted(int levelIndex);
}

public interface IProgressStorage
{
    SaveData Load();
    void Save(SaveData data);
}

public sealed class ProgressService : IProgress
{
    public ProgressService(IProgressStorage storage, int levelCount)
    {
        this.storage = storage;
        data = storage.Load() ?? new SaveData();
        // Güncellemede bölüm silindiyse sınırla
        data.HighestCompletedIndex = Math.Min(data.HighestCompletedIndex, levelCount - 1);
    }

    public bool IsCompleted(int i) => i <= data.HighestCompletedIndex;
    public bool IsUnlocked(int i)  => i <= data.HighestCompletedIndex + 1;

    public void MarkCompleted(int i)
    {
        if (i <= data.HighestCompletedIndex) return;   // tekrar oynama: yazma yok
        data.HighestCompletedIndex = i;
        storage.Save(data);
    }
}
```
- Kilit kuralı: **`i <= HighestCompletedIndex + 1` ise açık.** İlk bölüm (`-1 + 1 = 0`) her zaman açık.
- `ProgressService`, arayüzler ve `InMemoryProgressStorage` **Core'da** (saf C#, test edilebilir). `[Serializable]` = `System.Serializable`.
- `InMemoryProgressStorage`: veriyi sadece bellekte tutar. Testlerde, Faz 4'te (kalıcı kayıt gelmeden önce) ve editörden "Bu bölümü oyna" ile başlatıldığında (§15.3) kullanılır.

### 8.3 `PlayerPrefsProgressStorage` (Runtime) — seçilen depolama
```csharp
public sealed class PlayerPrefsProgressStorage : IProgressStorage
{
    public const string DefaultKey = "progress";
    private readonly string key;

    // Anahtar enjekte edilebilir: testler gerçek kaydı ezmesin diye kendi anahtarını kullanır.
    public PlayerPrefsProgressStorage(string key = DefaultKey) => this.key = key;

    public SaveData Load()
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new SaveData();
        try   { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
        catch { return new SaveData(); }   // bozuk kayıt: sıfırdan başla, çökme
    }

    public void Save(SaveData data)
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();                // OnApplicationQuit'e GÜVENİLMEZ (mobilde çağrılmayabilir)
    }
}
```

### 8.4 Davranış tablosu
| Durum | Davranış |
|---|---|
| Bölüm kazanıldı | `WinState.Enter` içinde `MarkCompleted(n)` → hemen diske yazılır |
| Geçilmiş bölüm tekrar kazanıldı | Yazma yok, ilerleme değişmez |
| Bölüm ortasında uygulama kapandı | Bölüm içi durum **kaydedilmez**; oyuncu bölümü baştan oynar |
| Kayıt bozuk/okunamıyor | Varsayılan `SaveData` |
| Kayıttaki değer katalogdan büyük | `levelCount - 1` ile sınırlanır |

### 8.5 İleride (şimdi değil)
JSON dosyası veya bulut kaydı gerekirse yeni bir `IProgressStorage` yazılır; başka hiçbir kod değişmez.

---

## 9. Dünya Yerleşimi, Kamera ve Input

### 9.1 Yerleşim (dikey ekran, kameradan bakınca)
```
         ← çıkış          [ OTOBÜS ]         giriş ←      z = BusZ       (otobüs sağdan gelir, sola gider)
                              ▲ kapı noktası
              [s0] [s1] [s2] [s3] [s4]                    z = SlotsZ     (bekleme koltukları, ortalanmış)
   ─────────────────────── yol ───────────────────────    z = RoadZ
              [  tahta satır 0 (y = 0)  ]                 z = 0
              [  tahta satır 1          ]                 z = -1 × CellSize
              ...                                          (tahta -z yönünde uzar)
```

### 9.2 `LayoutSettings` (ScriptableObject) — varsayılan değerler
| Alan | Varsayılan | Anlamı |
|---|---|---|
| `CellSize` | 1.0 | Hücre kenarı (dünya birimi) |
| `RoadOffset` | 1.0 | Yolun tahtanın üst kenarına uzaklığı (hücre cinsinden) |
| `SlotsOffset` | 2.0 | Koltuk sırasının uzaklığı |
| `BusOffset` | 3.5 | Otobüsün park noktası |
| `SlotSpacing` | 1.1 | Koltuklar arası mesafe |
| `DoorOffsetZ` | 0.6 | Kapı noktasının otobüsün önüne (−z) uzaklığı |
| `BusOffscreenX` | 10 | Otobüsün ekran dışı başlangıç/bitiş x'i |
| `BackdropPadding` | 0.2 | Tahta ve koltuk zeminlerinin taştığı pay (hücre cinsinden) |
| `BusQueueOffsetX` | 2.7 | Sıradaki otobüsün park noktasının sağında beklediği mesafe (otobüs boyu 2.4 + küçük boşluk) |

### 9.3 `BoardLayout` (Runtime/View, saf hesap sınıfı)
Bir `LevelData` ve `LayoutSettings` ile kurulur. **Tüm dünya koordinatı matematiği burada ve sadece burada.**
```csharp
public sealed class BoardLayout
{
    public BoardLayout(LevelData level, LayoutSettings s);

    // Tahta x'te ortalanır, üst satır z = 0'da.
    public Vector3 Origin => new Vector3(-(width - 1) / 2f * s.CellSize, 0f, 0f);

    public Vector3 CellToWorld(GridPos p) =>
        Origin + new Vector3(p.X * s.CellSize, 0f, -p.Y * s.CellSize);

    public Vector3 RoadPointAbove(GridPos exitCell) =>
        new Vector3(CellToWorld(exitCell).x, 0f, s.RoadOffset * s.CellSize);

    public Vector3 SlotPosition(int slot) =>
        new Vector3((slot - (slotCount - 1) / 2f) * s.SlotSpacing, 0f, s.SlotsOffset * s.CellSize);

    public Vector3 BusParkPosition => new Vector3(0f, 0f, s.BusOffset * s.CellSize);
    public Vector3 DoorPoint       => BusParkPosition + new Vector3(0f, 0f, -s.DoorOffsetZ);
    public Vector3 BusEnterPosition => BusParkPosition + new Vector3( s.BusOffscreenX, 0f, 0f);
    public Vector3 BusExitPosition  => BusParkPosition + new Vector3(-s.BusOffscreenX, 0f, 0f);
// Zeminler (Bounds, y = 0): tahta zemini tahtayı + yolu kaplar, koltuk zemini koltuk sırasını; ikisi JoinZ'de birleşir.
// JoinZ = (RoadOffset + SlotsOffset) / 2 * CellSize
public Bounds BoardBackdrop { get; }
public Bounds StopBackdrop { get; }
public Vector3 BusQueuePosition => BusParkPosition + new Vector3(s.BusQueueOffsetX, 0f, 0f);    // sıradaki otobüs (ContentBounds'ta değil; bir kısmı görünür)
public Vector3 BusQueueEnterPosition => BusEnterPosition + new Vector3(s.BusQueueOffsetX, 0f, 0f); // takipçi, öndekinin bir kuyruk mesafesi arkasından girer
// Otobüs yolu (ContentBounds'ta değil): z'de StopBackdrop.max.z'den park noktasına simetrik; x'te BusExitPosition'dan
// BusQueueEnterPosition'a, iki uçta birer kuyruk mesafesi fazlasıyla.
public Bounds BusRoad { get; }

    // Dünya → hücre (tek bölme işlemi)
    public GridPos WorldToCell(Vector3 world) => new GridPos(
        Mathf.RoundToInt((world.x - Origin.x) / s.CellSize),
        Mathf.RoundToInt((Origin.z - world.z) / s.CellSize));

    public Bounds ContentBounds { get; }   // tahta + koltuklar + otobüs alanı (kamera için)
}
```

### 9.4 `CameraFitter`
- Sabit perspektif kamera: eğim **55°** (x rotasyonu), dikey FOV **40°**. (Ayarlanabilir alanlar.)
- Her bölüm başında `Fit(layout.ContentBounds)`:
```csharp
float vFov = cam.fieldOfView * Mathf.Deg2Rad;
float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov / 2f) * cam.aspect);

float distForWidth = (bounds.size.x / 2f) / Mathf.Tan(hFov / 2f);
float distForDepth = (bounds.size.z / 2f) / Mathf.Tan(vFov / 2f);
float distance = Mathf.Max(distForWidth, distForDepth) * (1f + margin);   // margin varsayılan 0.3

cam.transform.position = bounds.center - cam.transform.forward * distance;
```
Eğimden kaynaklanan perspektif farkını `margin` karşılar. Daha karmaşık bir hesap yapılmaz.
> **Neden 0.3?** Dikey ekranda sığdırma genişliğe göre yapılır; tahtanın kameraya en yakın (alt) satırı daha geniş görünür. 0.15 ile alt köşeler ve üzerlerindeki ~1 birim boyundaki nesneler 9:16 ekranda yanlardan ~%8 taşıyordu. 0.3 ile 9:16 ve 19.5:9 telefonlarda 5 bölümün tamamı (1 birim yüksekliğe kadar) ekrana sığar.

### 9.5 `InputHandler`
- **Input System `Pointer`** soyutlaması: telefonda parmak, editörde fare. Tek kod iki ortamda çalışır.
- `Pointer.current` dokunmatikte **birincil parmaktır**; ikinci parmak ikinci bir dokunuş üretmez.
- Dokunuş, parmak **ekrana değdiği anda** (`wasPressedThisFrame`) işlenir.
- Fizik/collider **kullanılmaz:** kameradan çıkan ışın `y = 0` düzlemiyle kesiştirilir.

```csharp
[DefaultExecutionOrder(100)]   // EventSystem'den SONRA çalışsın: IsPointerOverGameObject bu karenin dokunuşunu bilsin
public sealed class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    public event Action<GridPos> CellTapped;

    private BoardLayout layout;
    private readonly Plane ground = new Plane(Vector3.up, Vector3.zero);

    public void SetLayout(BoardLayout layout) => this.layout = layout;

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());
        if (!ground.Raycast(ray, out float distance)) return;

        CellTapped?.Invoke(layout.WorldToCell(ray.GetPoint(distance)));
    }
}
```
- Hücre tahta dışındaysa `GameSession.Tap` zaten hiçbir şey yapmaz (yolcu yok).
- **Açma/kapama:** bayrak yok; `inputHandler.enabled = true/false`. Kapalıyken Unity `Update`'i hiç çağırmaz.
- **Başlangıçta kapalı:** Sahnede bileşen işaretsiz (disabled) durur ve `GameBootstrap` da `enabled = false` yapar. Sadece `GameplayState.Enter` açar (aksi halde menüde `layout == null` iken dokunuş `NullReferenceException` verir).
- `[DefaultExecutionOrder(100)]`: UI'ya (ör. HUD'daki baştan başlat butonuna) yapılan dokunuşun tahtaya da geçmemesi için. Cihazda yine de sızıntı görülürse yedek çözüm: `EventSystem.current.RaycastAll` ile UI isabeti kontrolü.
- `IsPointerOverGameObject` bilinçli olarak `Update` içinde (Input System callback'i içinde çağrılırsa güncel olmayan sonuç verir).
- Input okumak için `Update` kullanmak, "Update'te durum yoklama yok" kuralının bilinçli istisnasıdır. Maliyeti ihmal edilebilir (her karede bir null kontrolü + bir karşılaştırma, tahsis yok).

### 9.6 `BackButtonInput`
- Input System'de Android geri tuşu `Keyboard.current.escapeKey` olarak gelir (editörde Esc tuşu aynı işi görür).
```csharp
private void Update()
{
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        BackPressed?.Invoke();
}
```
- Durumlar `Enter`'da abone olur, `Exit`'te aboneliği kaldırır (§11).

### 9.7 Gerekli sahne ayarı
`EventSystem` üzerinde **`InputSystemUIInputModule`** (eski `StandaloneInputModule` kaldırılır).

---

## 10. View Katmanı ve Animasyonlar

### 10.1 Temel kurallar
- View sınıfları **sadece görüntüden** sorumlu; kural bilmez, karar vermez.
- **Yolcu yürüyüşleri paralel**; oyuncu istediği hızda dokunur.
- **Sadece otobüs olayları sıralı** (`BusLane`).
- Tüm tween'ler **DOTween**. Coroutine kullanılmaz.
- **Tween referansı saklanmaz.** DOTween tween'leri geri kullanır (recycling); saklanan bir referans sonradan başka bir tween'i gösterebilir. Bitişi bilmek gereken kod `OnComplete` içinde **kendi bayrağını** set eder ya da bir `Action` callback'i alır. Tween öldürmek gerektiğinde hedef üzerinden yapılır: `transform.DOKill()`.
- **Her transform katmanının tek sahibi var** (§10.6). Böylece aynı özelliği iki animasyon aynı anda değiştirmez.
- Havuza dönen her görsel `ResetForPool()` içinde kendi transform'larının tween'lerini `DOKill()` ile öldürür. Ek güvenlik olarak dünya nesnelerinde **en dıştaki** tween/Sequence'e `.SetLink(gameObject, LinkBehaviour.KillOnDisable)` eklenir (Sequence içindeki alt tween'lere değil).

### 10.2 Prefab'lar, havuzlar ve sahiplik
| Prefab | İçerik |
|---|---|
| `Passenger` | Katmanlı hiyerarşi (§10.6) + blob gölge quad'ı |
| `Bus` | Gövde + koltuklar için boş bir `SeatRoot` (tavan yüksekliğinde) |
| `BusSeat` | Tavandaki tek koltuk; kökü tavan hizasında. `Chair`: koyu, alçak kutu (tek ortak malzeme `SeatChair`, Simple Lit, gölgesiz), tavanın üstünde. `Passenger`: yolcu mesh'i, tavana yarıya kadar gömülü (kökün 0.42 altında), gölgesiz. Script'siz. Kök ölçeklenince koltuk, yolcu ve gömülme derinliği birlikte küçülür |
| `FloorTile`, `WallBlock`, `EdgePiece`, `SlotMarker` | Statik tahta parçaları |

- Havuz: Unity'nin yerleşik **`UnityEngine.Pool.ObjectPool<T>`** sınıfı. Ayrı bir havuz sınıfı yazılmaz.
- **Sahiplik:** Yolcu ve otobüs havuzlarının sahibi **`GameplayPresenter`**; tahta parçalarının sahibi `BoardView`; koltuk işaretlerinin sahibi `WaitingAreaView`. Bir görseli **sadece sahibi** havuza geri verir. Adımlar (`BoardStep`, `BusDepartStep`) havuza doğrudan dokunmaz; sahibin `ReleasePassenger(id)` / `ReleaseBus(id)` metodunu çağırır. Bu metotlar sözlükten de siler; böylece `Clear()` aynı nesneyi ikinci kez havuza vermez.
- Havuzdan alınırken: `SetActive(true)`. Havuza verilirken: `ResetForPool()` (tween'leri öldür, tüm katmanların yerel konum/rotasyon/ölçeğini sıfırla, oturan yolcuları gizle) + `SetActive(false)`.
- **Ön ısıtma (pre-warm):** Her havuz sahibinin bir `Prewarm()` metodu var (`GameplayPresenter`, `BoardView`, `WaitingAreaView`); `GameBootstrap` açılışta bunları çağırır. Her biri kendi havuzundan N nesne alıp hemen geri verir; ilk bölümün intro'sunda `Instantiate` olmaz. Varsayılan N: Passenger 40, Bus 4, FloorTile 64, WallBlock 8, EdgePiece 64, SlotMarker 8.
- Bölümler arasında nesneler **yok edilmez**, havuza döner.
- Şu anki modeller (Faz 8) kodla üretilmiş yer tutucu low-poly mesh'lerdir (`Content/Models/`: `Passenger` ~400 üçgen, `BusBody` + `BusDetails`). Gerçek modeller geldiğinde prefab'daki `Model` / `Body` / `Details` nesnelerinin mesh'i değiştirilir; pivot ayakta (y = 0) kalmalıdır. Otobüs -X yönüne (sola) gider; ön cam o taraftadır. Duvar (`WallBlock`) görüşü kapatmasın diye çok basık bir yarım küredir (`Content/Models/WallDome`: 0,9 çap, 0,18 yükseklik).

### 10.3 `ColorPalette` (ScriptableObject)
`PassengerColor` → `Material` (yolcu ve otobüs ortak) ve `Color` (UI).
- **Renk başına bir materyal** (URP Simple Lit). `MaterialPropertyBlock` **kullanılmaz** (SRP Batcher uyumluluğunu bozar).

### 10.4 `BoardView` ve `WaitingAreaView`
```csharp
// BoardView: prefab'lar [SerializeField] ile
public void Init(LayoutSettings layoutSettings);   // bootstrap'te bir kez
public void Prewarm();
public void Build(LevelData level, BoardLayout layout);
public void Clear();

// WaitingAreaView: SlotMarker prefab'ı [SerializeField] ile
public void Init(LayoutSettings layoutSettings);
public void Prewarm();
public void Build(int slotCount, BoardLayout layout);
public void Clear();
```
- `Floor` → `FloorTile`; `Wall` → `FloorTile` + `WallBlock`; `Void` → **hiçbir şey.**
- **Zeminler:** `BoardView` tahtanın altına (`layout.BoardBackdrop`), `WaitingAreaView` koltuk sırasının altına (`layout.StopBackdrop`) birer `Backdrop` koyar (koyu gri-mavi, Simple Lit, gölgesiz; üst yüzü karoların hemen altında). İki zemin arada boşluk kalmadan birleşir, böylece tahta ve koltuklar bitişik görünür; otobüs şeridi zeminlerin dışında kalır. Zemin nesnesi `Init`'te bir kez oluşturulur, `Build` yerleştirir, `Clear` gizler. `ContentBounds` zeminleri de kapsar.
- **Yol:** `WaitingAreaView` otobüs şeridinin altına bir `Road` (koyu asfalt, `Backdrop` ile aynı küp, Simple Lit, gölgesiz) koyar. Yol `layout.BusRoad`: z'de koltuk zemininin üst kenarından başlar (boşluksuz birleşir) ve park noktasına göre simetriktir; x'te otobüslerin doğduğu ve kaybolduğu noktaları (kuyruktaki otobüs dahil) aşar, böylece ekranı yatayda her en-boy oranında baştan sona kaplar. Yol `ContentBounds`'a girmez (kamera ona göre uzaklaşmaz). `Init`'te bir kez oluşturulur, `Build` yerleştirir, `Clear` gizler. Durak tabelası denendi, ekip kararıyla kaldırıldı.
- **Kenarlar:** Her `Floor`/`Wall` hücresi için 4 yönün her birinde komşu `!Exists` ise o kenara bir `EdgePiece` (yönüne göre döndürülmüş) konur. **İstisna:** çıkış hücresinin (§2.4, `Board.IsExit`) **üst** kenarı çizilmez; yolcular yola o taraftan çıkar. Ortadaki deliğin altındaki hücre çıkış olmadığı için onun üst kenarı çizilir; duvarların kenarları değişmez. Karar `BoardView.HasEdge(board, cell, direction)` statik metodunda. Auto-tiling algoritması yok.

### 10.5 `GameplayPresenter : MonoBehaviour, IGameListener`
Core olaylarını **serbest animasyona** ya da **`BusLane`'e** yönlendirir. (Konuşmada `AnimationQueue` olarak geçen sınıf; artık tek kuyruk olmadığı için adı değişti.)

```csharp
public event Action<SessionResult> PresentationFinished;

public void Init(ColorPalette palette, AnimationSettings anim, LayoutSettings layoutSettings, IFeedback feedback);   // bootstrap'te bir kez
public void Begin(LevelData level, BoardLayout layout);   // yolcu görsellerini kurar, intro
public void Clear();                                       // her şeyi sıfırlar (aşağıda)
public void Prewarm();
public void ReleasePassenger(int passengerId);
public void ReleaseBus(int busId);
```

**`Begin`:** `level.Passengers` (`PassengerSpawn`: Id, Color, Position) üzerinden her yolcu için havuzdan bir `PassengerView` alınır, `Init(spawn, material, layout.CellToWorld(spawn.Position))` ve `PlayIntro(delay: spawn.Id * IntroStagger)` çağrılır. Görseller Core nesnelerine değil **Id'lere** bağlıdır; bu yüzden `Begin`, `GameSession`'dan önce çağrılabilir. Otobüs görselleri burada **oluşturulmaz** (bkz. `OnBusArrived`).

**`Clear`** — şu durumların **hepsini** sıfırlar:
- `lane.Clear()`
- Sözlükteki tüm yolcu ve otobüs görselleri `ResetForPool` ile havuza (sözlükler boşaltılır)
- `fromSlot` işaret kümesi, `lastExitedId`
- `walkingCount = 0`
- `pendingResult = SessionResult.Playing`, `finishedRaised = false`

**Olay → animasyon tablosu (kesin):**
| Olay | Ne yapılır | Tür |
|---|---|---|
| `OnBusArrived(bus)` | Otobüs kuyrukta zaten bekliyorsa o görsel kullanılır, yoksa havuzdan alınıp `BusEnterPosition`'a konur. Arkasında otobüs varsa (`bus.Id + 1 < BusColors.Count`) o da havuzdan alınıp `BusQueueEnterPosition`'a konur (renk `LevelData.BusColors`'tan). `lane.Enqueue(new BusArriveStep(busView, BusParkPosition, follower, BusQueuePosition, feedback))` | Şerit |
| `OnPassengerBlocked(p)` | `view.PlayBlockedShake()` + `feedback.PassengerBlocked(pos)` | Serbest |
| `OnPassengerExited(p, path)` | `lastExitedId = p.Id`; `view.StopIdle()`, `view.PlayTapSquash()` + `feedback.PassengerTapped(pos)`; yol hücreleri dünya noktalarına çevrilir, sonuna `RoadPointAbove(path[path.Count - 1])` eklenir ve **saklanır** (`exitWalk`). Yürüyüş, aynı dokunuşun bir sonraki olayıyla başlar; yolcunun nereye gideceğine o olay karar verir | Serbest |
| `OnPassengerWaiting(p, slot)` | Durağa git: ekranda boş bir koltuk alınır (tercihen mantıktaki `slot`, doluysa en küçük boş koltuk; `StopSeats`), `exitWalk` + koltuk noktası yürünür, varınca `ArrivedAtSlot`: `feedback.PassengerWaiting()`; kayıp bekleniyorsa ve bu son çıkan yolcuysa `view.PlayBlockedShake()`. Ekranda boş koltuk yoksa yolcu hücresinde **tutulur** (aşağıdaki durak kuralı) | Serbest |
| `OnPassengerLeftWaitingArea(p, slot)` | Görsel bir şey yok: yolcu, `BoardStep` onu otobüse yürütene kadar ekrandaki koltuğunu korur | — |
| `OnPassengerBoarded(p, bus, seat)` | Yolcu tahtadan geliyorsa: otobüs **park etmişse** (`busView.IsParked`) `exitWalk` + `DoorPoint` yürünür; **park etmemişse** durağa gider (boş koltuk, yoksa tutulur). Zaten duraktaysa (koltukta ya da tutulmuş) yürüyüş yok; tutulmuşsa otobüsü kaydedilir. Her durumda `busView.ExpectBoarding()` ve `boardings.Add(new BoardStep(this, view, busView, feedback))` (şeride değil, paralel biniş listesine) | Serbest |
| `OnBusDeparted(bus)` | `lane.Enqueue(new BusDepartStep(this, busView, layout.BusExitPosition, feedback))` | Şerit |
| `OnWon()` | `pendingResult = Won` | — |
| `OnLost()` | `pendingResult = Lost` (sallanmayı `ArrivedAtSlot` yapar) | — |

**Durak kuralı (kesin):** Kimse kapıda otobüs beklemez ve ayrı bir bekleme yeri yoktur. Yolcu kapıya sadece otobüsü **park etmişken** yürür; aksi halde duraktaki boş bir koltuğa oturur ve otobüs park edince oradan biner. Otobüs park ettiği an, ona binecek ve koltukta bekleyen **bütün yolcular aynı anda kalkıp otobüse koşar**; her yolcu kapıya vardığı an biner (kapıda kimse kimseyi beklemez). Ekrandaki koltukları `StopSeats` dağıtır: koltuk, yolcusu gerçekten otobüse yürüyene kadar dolu sayılır, böylece iki yolcu aynı koltuğa düşmez. Bu yüzden ekrandaki koltuk mantıktaki koltuk numarasından farklı olabilir. Otobüs değişimi sırasında ekranda boş koltuk kalmazsa (nadir) yolcu hücresinde tutulur ve bir koltuk boşalınca ya da otobüsü park edince yürümeye başlar; o kısa sürede arkasındaki bir yolcu onun içinden geçerek yürüyebilir (kabul edildi). Kurallar (`GameSession`) bu görsel beklemeden etkilenmez; kaybetme yalnızca mantıktaki koltuklara bakar.

**Kayıp görseli:** Son dokunulan yolcu son boş koltuğa yürür, oturduğu anda sallanır. Popup, bu yürüyüş bittikten sonra açılır (sallanma beklenmez).

**Bitiş tespiti:**
```csharp
private void Update()
{
    lane.Tick();
    for (int i = boardings.Count - 1; i >= 0; i--)   // binişler paralel: her biri kendi hazır olunca başlar
    {
        boardings[i].Tick();
        if (boardings[i].IsDone) boardings.RemoveAt(i);
    }

    if (pendingResult != SessionResult.Playing && !finishedRaised
        && lane.IsIdle && boardings.Count == 0 && walkingCount == 0 && held.Count == 0)
    {
        finishedRaised = true;
        PresentationFinished?.Invoke(pendingResult);
    }
}
```
`walkingCount`: Her `PassengerView`'ın `WalkStarted` olayında +1, `WalkFinished` olayında −1. `Clear()` sayacı sıfırlar (öldürülen yürüyüşler `WalkFinished` yayınlamaz).

### 10.6 `PassengerView`

**Katmanlı hiyerarşi — her katmanın tek sahibi:**
```
Passenger (kök)      → konum + rotasyon:  yürüyüş (DOPath), otobüse zıplama (DOJump)
└── Bounce           → yerel konum:       yürürken adım zıplaması, kapalı yol sallanması
    └── Body         → yerel ölçek:       intro pop, dokunma squash'ı, otobüse girerken küçülme
        └── Mesh     → yerel ölçek:       idle "nefes"
```
Aynı katmanda yeni bir tek seferlik animasyon başlamadan önce o katmanın önceki tween'i tamamlanır: `body.DOKill(complete: true)`, `bounce.DOKill(complete: true)`. Sonsuz döngüler (adım zıplaması, nefes) ayrı katmanlarda olduğu için bunlardan etkilenmez.

```csharp
public int Id { get; private set; }
public void Configure(AnimationSettings anim, float cellSize);   // havuzun oluşturma fonksiyonunda bir kez
public void Init(PassengerSpawn spawn, Material material, Vector3 position);
public void PlayIntro(float delay);        // Body ölçeği 0 → 1, Ease.OutBack; bitince PlayIdle()
public void PlayIdle();                    // idleStopped ise hiçbir şey yapmaz; değilse Mesh: nefes, sonsuz Yoyo
public void StopIdle();                    // idleStopped = true; Mesh.DOKill(); Mesh ölçeği 1
public void PlayTapSquash();               // Body.DOKill(true); Body.DOPunchScale(squash, TapSquashTime)
public void PlayBlockedShake();            // Bounce.DOKill(true); Bounce.DOShakePosition(BlockedShakeTime, BlockedShakeStrength)
public void WalkAlong(Vector3[] points, Action onArrived = null);   // sıraya ekler
public bool IsWalking { get; private set; }
public event Action WalkStarted, WalkFinished;
public void HopInto(Vector3 doorPoint, Action onComplete);
public void MoveFromSlotTo(Vector3 doorPoint, Action onComplete);
public void ResetForPool();
```
**`idleStopped` bayrağı:** `PlayTapSquash` içindeki `Body.DOKill(complete: true)` intro tween'ini tamamlar ve intro'nun `OnComplete`'i `PlayIdle()`'ı çağırır. Bayrak sayesinde tahtadan çıkmış bir yolcuda nefes animasyonu yeniden başlamaz.

**`ResetForPool` şunların hepsini sıfırlar** (öldürülen tween'ler `OnComplete` çağırmaz, bu yüzden durum elle temizlenir):
- Tüm katmanlarda `DOKill()`; kök/Bounce/Body/Mesh yerel konum, rotasyon, ölçek varsayılana
- `pending.Clear()`, `IsWalking = false`, `idleStopped = false`
- (`WalkStarted`/`WalkFinished` abonelikleri **silinmez**: presenter bunlara havuzun oluşturma fonksiyonunda bir kez abone olur. Öldürülen yürüyüş `WalkFinished` yayınlamadığı için presenter `Clear()`'da `walkingCount`'u sıfırlar.)

**Yürüyüş kuyruğu:** DOTween'de **çalışmaya başlamış bir Sequence'e tween eklenemez.** Bu yüzden yürüyüşler bir parça kuyruğuyla yönetilir:
```csharp
private readonly Queue<(Vector3[] points, Action onArrived)> pending = new Queue<(Vector3[], Action)>();

public void WalkAlong(Vector3[] points, Action onArrived = null)
{
    pending.Enqueue((points, onArrived));
    if (!IsWalking) StartNextSegment();
}

private void OnSegmentFinished(Action onArrived)
{
    if (pending.Count == 0)
    {
        // Önce yürüyüşü bitir, SONRA onArrived: onArrived'in Bounce'a koyduğu
        // sallanmayı StopStepBounce'un DOKill'i iptal etmesin.
        IsWalking = false;
        StopStepBounce();
        WalkFinished?.Invoke();
        onArrived?.Invoke();
        return;
    }
    onArrived?.Invoke();
    StartNextSegment();
}

private void StartNextSegment()
{
    if (!IsWalking) { IsWalking = true; StartStepBounce(); WalkStarted?.Invoke(); }

    var (points, onArrived) = pending.Dequeue();
    float duration = PathLength(transform.position, points) / (anim.WalkSpeed * cellSize);

    transform.DOPath(points, duration, PathType.Linear)
             .SetLookAt(0.01f)
             .SetEase(Ease.Linear)
             .SetLink(gameObject, LinkBehaviour.KillOnDisable)
             .OnComplete(() => OnSegmentFinished(onArrived));
}

// Uzunluk yolcunun BULUNDUĞU yerden ölçülür; tek noktalı parçalar da doğru süre alır.
private static float PathLength(Vector3 from, Vector3[] points)
{
    float length = 0f;
    Vector3 previous = from;
    foreach (Vector3 p in points) { length += Vector3.Distance(previous, p); previous = p; }
    return length;
}
```
- `WalkStarted`/`WalkFinished` sadece kuyruk boştan doluya ve doludan boşa geçerken yayınlanır.
- Adım zıplaması: `bounce.DOLocalMoveY(StepHeight, StepTime).SetLoops(-1, LoopType.Yoyo)`; durdurulurken `bounce.DOKill()` ve yerel y = 0.
- `HopInto`: kök `DOJump(doorPoint, BoardHopHeight, 1, BoardHopTime)` ile birlikte `Body` ölçeği → 0; ikisinin birlikte bittiği an `onComplete`.
- `MoveFromSlotTo`: kök `DOMove(doorPoint, BoardFromSlotTime)`, bitince `onComplete`.
- **Tüm yolcular aynı hızda yürür.** Arka arkaya dokunulan yolcular ortak yolda birbirini geçemez.

### 10.7 `BusView`
```csharp
public int Id { get; private set; }
public Vector3 DoorPoint { get; private set; }
public bool IsParked { get; private set; }                     // Arrive bitince true; Init, Depart ve ResetForPool'da false
public event Action<BusView> Parked;                           // Arrive bitince (presenter tutulan yolcuları kapıya yollar)
public void Configure(AnimationSettings anim);                 // havuzun oluşturma fonksiyonunda bir kez
public void Init(int busId, Material material, int capacity, Vector3 enterPosition, Vector3 doorPoint);   // SetActive(true), konum, koltuklar (hepsi gizli, otobüs malzemesiyle)
public void Arrive(Vector3 parkPosition, Action onComplete);    // DOMove, Ease.OutQuad, BusArriveTime (önce kendi tween'ini öldürür)
public void DriveTo(Vector3 position);                          // kuyruğa ilerleme; park sayılmaz
public void Depart(Vector3 exitPosition, Action onComplete);    // DOMove, Ease.InQuad, BusDepartTime
public void SetFilledCount(int count);                          // ilk `count` koltuktaki yolcu görünür; yeni görünen DOPunchScale
public int PendingBoardings { get; }                           // binmesi beklenen, henüz binmemiş yolcu sayısı
public void ExpectBoarding();                                  // OnPassengerBoarded'da +1
public int CompleteBoarding();                                 // yolcu girdi: -1, binen sayısını döndürür (koltuklar ve perde bunu kullanır)
public void ResetForPool();
```
- **Oturan yolcular (doluluk):** Biniş, otobüsün tavanındaki koltuklarda oturan yolcularla gösterilir. **Trick:** yolcular zaten koltuktadır, sadece gizlidir; her biniş bir sonrakini açar. Yürüyen yolcu kapıda havuza döner, koltuktaki ise otobüsle birlikte saklanan ayrı bir modeldir (yolcu otobüse bağlanmaz; iki havuzun ömrü ayrı kalır). `BusView` bir `BusSeat` listesi tutar; liste yetmezse prefab'dan eklenir (bir daha oluşturulmaz; `Passenger` çocuğu eklenirken bir kez bulunur). `Init`'te `capacity` kadar koltuk görünür (fazlası gizli), hepsinin yolcusu gizlenir ve otobüsün malzemesini alır (yolcu ve otobüs aynı malzemeyi paylaşır). Boş koltuklar biniş boyunca görünür kalır, yani kaç kişinin bineceğini gösterir. `SetFilledCount(n)` ilk `n` koltuktaki yolcuyu açar; binişler paralel olduğu için koltuklar biniş sırasına değil sayıya göre dolar. Koltuklar `SeatRoot` altında ortalanarak dizilir: `x = (i - (capacity - 1) / 2f) * SeatSpacing * fit`. **Otomatik sığdırma:** `fit = min(1, gövde uzunluğu / (capacity * SeatSpacing))`; kapasite tavana sığmazsa her `BusSeat` kökü `fit` ile ölçeklenir. Koltuk ve yolcu tek prefab'da olduğu için aralık, koltuk, yolcu ve yolcunun tavana gömülme derinliği aynı oranda küçülür; yolcunun tavanın üstünde kalan payı her kapasitede aynı oranda görünür (3 kişide `fit = 1`, değişiklik yok).

### 10.8 `BusLane` ve adımlar
```csharp
public interface IStep
{
    void Tick();          // hazırsa animasyonunu başlatır (bir kez)
    bool IsDone { get; }  // kendi bayrağı; OnComplete'te set edilir
}

public sealed class BusLane
{
    private readonly Queue<IStep> steps = new Queue<IStep>();

    public bool IsIdle => steps.Count == 0;
    public void Enqueue(IStep step) => steps.Enqueue(step);
    public void Clear() => steps.Clear();

    public void Tick()
    {
        while (steps.Count > 0)
        {
            IStep step = steps.Peek();
            step.Tick();
            if (!step.IsDone) return;
            steps.Dequeue();
        }
    }
}
```
**Örnek adım:**
```csharp
// Şeritte DEĞİL: presenter'ın paralel biniş listesinde tick edilir.
public sealed class BoardStep : IStep
{
    // (constructor: presenter, passenger, bus, feedback)
    private bool started;
    public bool IsDone { get; private set; }

    public void Tick()
    {
        if (started || !bus.IsParked || passenger.IsWalking || presenter.IsHeldBack(passenger.Id)) return;   // otobüs durmadan / yürüyüş bitmeden / tutulmuşken başlama
        started = true;

        if (presenter.LeaveSeat(passenger.Id)) passenger.MoveFromSlotTo(bus.DoorPoint, Hop);   // koltuktaysa oradan
        else Hop();
    }

    private void Hop() => passenger.HopInto(bus.DoorPoint, OnBoarded);

    private void OnBoarded()
    {
        int boarded = bus.CompleteBoarding();    // sırasız biniş: koltuk numarası değil binen sayısı
        bus.SetFilledCount(boarded);
        feedback.PassengerBoarded(boarded - 1);
        presenter.ReleasePassenger(passenger.Id);
        IsDone = true;
    }
}
```
| Adım | Başlama koşulu | Ne yapar | Bitince |
|---|---|---|---|
| `BusArriveStep` | Sıra gelince | `busView.Arrive(park, ...)` (kuyruktan kısa ilerleme ya da sağdan giriş); varsa arkadaki otobüs aynı anda `DriveTo(BusQueuePosition)` | `IsParked = true`, `Parked` olayı, `feedback.BusArrived()` |
| `BoardStep` (paralel, şeritte değil) | Otobüs park etti **ve** yolcunun yürüyüşü bitti **ve** yolcu tutulmuyor | Ekranda koltuktaysa koltuğu bırakır (`presenter.LeaveSeat`, boşalan koltuk ilk tutulan yolcuya verilir) + `MoveFromSlotTo` + `HopInto`, değilse sadece `HopInto` | `CompleteBoarding()` → `SetFilledCount(binen)`, `feedback.PassengerBoarded(binen - 1)`, `presenter.ReleasePassenger(id)` |
| `BusDepartStep` | Sıra geldi **ve** otobüse binecek herkes bindi (`PendingBoardings == 0`) | `feedback.BusDeparted(pos)` + `busView.Depart(exit, ...)` | `presenter.ReleaseBus(id)` |

- Şerit yalnızca otobüs geliş/gidişlerini sıralar. **Binişler paraleldir:** her yolcu, otobüsü park etmişken kapıya vardığı an biner; kimse sırasını kapıda beklemez. Otobüs park edince koltuktaki yolcuları aynı anda kalkar. Otobüs, ona binecek son yolcu girince kalkar.
- Aynı anda kapıya varan yolcuların bir an üst üste görünmesi **kabul edilmiş bir durumdur** (§19).

### 10.9 `AnimationSettings` (ScriptableObject) — varsayılan değerler
Hepsi Play modunda değiştirilip denenebilir; **kodda sabit süre/hız/güç yazılmaz.**
| Alan | Varsayılan |
|---|---|
| `WalkSpeed` | 4.0 (hücre/sn) |
| `StepHeight` / `StepTime` | 0.15 / 0.12 sn |
| `TapSquash` / `TapSquashTime` | (0.2, −0.2, 0.2) / 0.2 sn |
| `BlockedShakeStrength` / `BlockedShakeTime` | 0.1 / 0.3 sn |
| `BoardHopHeight` / `BoardHopTime` | 0.5 / 0.2 sn |
| `BoardFromSlotTime` | 0.25 sn |
| `BusArriveTime` / `BusDepartTime` | 0.5 / 0.4 sn |
| `IntroPopTime` / `IntroStagger` | 0.25 / 0.02 sn |
| `IdleBreathScale` / `IdleBreathTime` | 0.03 / 1.2 sn |
| `SeatSpacing` | 0.7 |
| `PopupTime` | 0.25 sn |
| `NodePulseScale` / `NodePulseTime` | 1.1 / 0.6 sn |

### 10.10 DOTween başlangıç ayarı (`GameBootstrap.Awake`)
```csharp
DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
       .SetCapacity(200, 50);
```
- Tween'ler geri kullanılır (bu yüzden §10.1: tween referansı saklanmaz), kapasite baştan ayrılır.
- `OnComplete` lambda'ları dokunuş başına küçük tahsis yapar; kabul edilir. Hedef: **her karede** sıfır tahsis (§16).

### 10.11 Animasyon listesi (özet)
| Nesne | Animasyon | Katman | Tür |
|---|---|---|---|
| Yolcu | Intro pop | Body | Serbest |
| | Idle nefes | Mesh | Serbest, sürekli |
| | Dokunma squash | Body | Serbest |
| | Kapalı yol sallanması | Bounce | Serbest |
| | Yürüyüş | Kök | Serbest |
| | Adım zıplaması | Bounce | Serbest, yürürken |
| | Otobüse zıplayıp küçülme | Kök + Body | Şerit |
| Otobüs | Geliş / koltukta beliren yolcu / gidiş | Kök / SeatRoot (yeni koltuk) | Şerit |
| UI | Popup ölçek 0.8 → 1 + fade | Panel | UI (`SetUpdate(true)`) |
| UI | Harita mevcut düğüm nabzı | Düğüm | UI, sonsuz Yoyo |

---

## 11. Uygulama Akışı (Durum Makinesi)

### 11.1 İki ayrı "durum" kavramı
- **`GameSession.Result`**: tek bir bölümün içindeki kural durumu (Playing/Won/Lost).
- **Uygulama durumları** (`IAppState`): oyuncu şu an **hangi ekranda?** Bu bölüm bunu anlatır.

Oyuncu **her an tam olarak bir** uygulama durumundadır. Her durum, girişte (`Enter`) ve çıkışta (`Exit`) ne yapacağını ve hangi duruma geçilebileceğini bilir.

### 11.2 Diyagram
```
               bölüm seç (açıksa)
   ┌────────┐ ─────────────────────▶ ┌──────────┐ ◀── baştan başlat (HUD) ──┐
   │  Menü  │                        │  Oyun(n) │ ──────────────────────────┘
   │(harita)│ ◀── geri tuşu ──────── └──────────┘
   └────────┘                     kazandı │      │ kaybetti
     ▲    ▲                               ▼      ▼
     │    │   ana menü / geri   ┌──────────┐   ┌───────────┐
     │    └──────────────────── │Kazandı(n)│   │Kaybetti(n)│
     │                          └──────────┘   └───────────┘
     │         sonraki → Oyun(n+1)          tekrar dene → Oyun(n)
     └──────────────────────── ana menü / geri ─────────┘
```

### 11.3 Sözleşme ve makine
```csharp
public interface IAppState
{
    void Enter();
    void Exit();
}

public sealed class AppStateMachine
{
    private IAppState current;

    public void ChangeTo(IAppState next)
    {
        current?.Exit();
        current = next;
        current.Enter();
    }
}
```

### 11.4 `AppStates` (durum fabrikası)
Durumların bağımlılıklarını tek yerde tutar ve yeni durum nesneleri üretir. Durumlar birbirine `states.Gameplay(n)` gibi çağrılarla geçer.
```csharp
public sealed class AppStates
{
    public AppStates(AppStateMachine machine, AppDependencies deps);   // AppDependencies: §14.2

    public IAppState Menu();
    public IAppState Gameplay(int levelIndex);
    public IAppState Win(int levelIndex);
    public IAppState Lose(int levelIndex);
}
```

### 11.5 Durumların davranışı (kesin)

**`MenuState`**
| | |
|---|---|
| Enter | Tahta görselleri temizlenir (`presenter.Clear()`, `boardView.Clear()`, `waitingAreaView.Clear()`); `mapScreen.Show(progress)` (düğüm durumlarını tazeler ve mevcut bölüme kaydırır); `mapScreen.LevelSelected`, `backButton.BackPressed` abonelikleri |
| Bölüm seçildi | `progress.IsUnlocked(i)` **tekrar kontrol edilir** (UI'daki bir hata kilitli bölümü açamasın); açıksa `ChangeTo(Gameplay(i))` |
| Geri tuşu (overlay kapalıyken) | `quitConfirmScreen` **overlay** olarak açılır: "Yes" → `Application.Quit()`, "No" → kapanır |
| Geri tuşu (overlay açıkken) | Overlay kapanır (çıkış yapılmaz) |
| Exit | Abonelikler kaldırılır, harita ve varsa çıkış popup'ı gizlenir |

**`GameplayState(n)`**
| | |
|---|---|
| Enter | 1) Önceki bölümün görselleri temizlenir (`presenter.Clear()`, `boardView.Clear()`, `waitingAreaView.Clear()`). 2) `level = catalog.Get(n)`, `layout = new BoardLayout(level, layoutSettings)`. 3) `boardView.Build`, `waitingAreaView.Build`, `cameraFitter.Fit(layout.ContentBounds)`. 4) `presenter.Begin(level, layout)`. 5) `session = new GameSession(level, pathFinder, presenter)`. 6) `input.SetLayout(layout)`, `input.CellTapped += session.Tap`, `input.enabled = true`. 7) `hud.Show()`, `hud.SetLevel(n)`. 8) `hud.RestartClicked`, `backButton.BackPressed`, `presenter.PresentationFinished` abonelikleri. 9) `session.Start()` |
| `PresentationFinished(Won)` | `ChangeTo(Win(n))` |
| `PresentationFinished(Lost)` | `ChangeTo(Lose(n))` |
| HUD "baştan başlat" | `ChangeTo(Gameplay(n))` — **sıfırlama yok, yeniden oluşturma var** |
| Geri tuşu | `ChangeTo(Menu())` |
| Exit | `input.enabled = false`; tüm abonelikler kaldırılır; `hud.Hide()`. **Tahta görselleri ve tween'leri burada temizlenmez** (bkz. 11.6). `DOTween.KillAll()` **kullanılmaz** (UI tween'lerini de öldürür) |

**`WinState(n)`**
| | |
|---|---|
| Enter | `progress.MarkCompleted(n)`; `winScreen.Show(showNextButton: catalog.HasNext(n))`; `feedback.Won()`; abonelikler |
| Sonraki | `ChangeTo(Gameplay(n + 1))` |
| Ana menü / geri tuşu | `ChangeTo(Menu())` |
| Exit | Abonelikler kaldırılır, `winScreen.Hide()` |

Son bölümde `HasNext` false olduğu için "Next" butonu gizlenir ve "All levels completed!" metni görünür; sadece "Main Menu" butonu kalır.

**`LoseState(n)`**
| | |
|---|---|
| Enter | `loseScreen.Show()`; `feedback.Lost()`; abonelikler |
| Tekrar dene | `ChangeTo(Gameplay(n))` |
| Ana menü / geri tuşu | `ChangeTo(Menu())` |
| Exit | Abonelikler kaldırılır, `loseScreen.Hide()` |

### 11.6 Kritik detay: tahta görselleri ne zaman temizlenir?
Kazandı/Kaybetti popup'ları **bitmiş tahtanın üstünde** açılır; arkada tahta görünmeye devam etmeli. Bu yüzden:
- `GameplayState.Exit` tahta görsellerini **temizlemez** ve tween öldürmez (sadece input'u kapatır, abonelikleri kaldırır, HUD'u gizler).
- Temizlik, bir sonraki **`GameplayState.Enter`** ya da **`MenuState.Enter`** başında `presenter.Clear()`, `boardView.Clear()`, `waitingAreaView.Clear()` ile yapılır. Her görsel havuza dönerken kendi tween'lerini öldürür (§10.2).

"Baştan başlat" ve "tekrar dene" de aynı yolu izler: `Exit` (input kapanır) → `Enter` (temizle, aynı `LevelData`'dan yeniden kur). `ChangeTo` senkron olduğu için arada hiçbir kare çizilmez.

### 11.7 Overlay kuralı
- **Ekranı değiştiren** şeyler durum geçişidir (menü → oyun → kazandı).
- **Mevcut ekranın üstüne açılıp kapanan** şeyleri ilgili durum kendisi yönetir. Şu an tek overlay: menüdeki çıkış onayı.
- Overlay'i durum geçişi yapmak, bulunulan durumun `Exit`'ini çalıştırıp bağlamı yok ederdi.

### 11.8 Input ne zaman açık?
Sadece `GameplayState` aktifken. Popup'lar açıkken ve menüdeyken `InputHandler.enabled = false`.

### 11.9 Kenar durumlar
| Durum | Davranış |
|---|---|
| Butona çift tıklama | `ScreenView.Fire` ilk tıklamada grubu pasif yapar; ikinci geçiş tetiklenmez |
| Animasyon ortasında "baştan başlat" | `Enter` → `presenter.Clear()`: lane boşalır, görseller tween'leri öldürülerek havuza döner, bitiş bayrakları sıfırlanır. Eski animasyon yeni bölümde oynamaz |
| Animasyon ortasında geri tuşu | `MenuState.Enter` aynı temizliği yapar |
| Mantıkta oyun bitti, animasyonlar sürüyor | Input açık kalır ama `GameSession` dokunuşları yok sayar (`IsOver`). Popup, animasyonlar bitince açılır |
| Uygulama arka plana alındı | Özel işlem yok |

---

## 12. UI Katmanı

### 12.1 Seçimler
- **UGUI + TextMeshPro.** (2022.3'te UI Toolkit'in runtime veri bağlama desteği yok; animasyonlu oyun UI'ı için UGUI daha uygun.)
- Her ekranın **kendi Canvas'ı** var. Gizleme `canvas.enabled = false` ile (GameObject kapatılmaz; yeniden açılınca UI baştan kurulmaz, ucuz).
- Canvas: Screen Space – Overlay. **Canvas Scaler:** Scale With Screen Size, referans **1080×1920**, Match **0.5**.
- **Butonlar kodda bağlanır** (`onClick.AddListener`), Inspector `OnClick` listesi kullanılmaz.
- UI → Flow: C# `event`'leri ("tıklandı"). Flow → UI: doğrudan metot (`Show`, `Hide`, `SetLevel`). UI, Flow'u tanımaz.

### 12.2 `ScreenView` (taban sınıf)
```csharp
public abstract class ScreenView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup group;
    protected IFeedback Feedback { get; private set; }

    // Butonlar burada (kodda) bağlanır. Awake değil: EditMode testlerinde Awake çalışmaz.
    public void Init(IFeedback feedback)
    {
        Feedback = feedback;
        BindButtons();
    }

    protected abstract void BindButtons();

    public virtual void Show()
    {
        canvas.enabled = true;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        group.interactable = false;
        group.blocksRaycasts = false;   // gizli popup'ın arka planı dokunuşları engellemesin
        KillTweens();                   // ekranın kendi tween'leri (popup paneli, fade vb.)
        canvas.enabled = false;
    }

    // Alt sınıflar kendi tween hedeflerini öldürür. Örn. popup: panel.DOKill(); group.DOKill();
    protected virtual void KillTweens() { group.DOKill(); }

    // SADECE kesin bir durum geçişi tetikleyen butonlar için (popup butonları):
    // çift tıklama koruması + tık sesi. Grup bir sonraki Show()'da yeniden açılır.
    protected void Fire(Action action)
    {
        group.interactable = false;
        Feedback?.UiClick();
        action?.Invoke();
    }
}
```
**`Fire` ne zaman kullanılmaz?** Sonucu kesin bir geçiş olmayan tıklamalarda: HUD butonu (tekrar basılabilir), harita düğümleri (kilitli düğüm geçiş yapmaz). Bunlar doğrudan `Feedback?.UiClick()` + event çağırır. Aksi halde kilitli bir düğüme dokunmak tüm haritayı tıklanamaz bırakır.

**Buton prefab'ları:** Tüm butonlar iki prefab'dan gelir: `Content/Prefabs/UI/Button.prefab` (`Button.png`) ve `WideButton.prefab` (`wideButton.png`); her biri buton görseli + `Label`. Basılınca `ButtonPress` butonu küçültür (`AnimationSettings.ButtonPressScale/Time`), bırakınca eski boyutuna döner; renk değişimi `Button`'ın ColorTint geçişidir.

Popup panelleri `panel 1.png` arka planını ve bir `VerticalLayoutGroup` (ortada, elemanlar arası sabit 25 px, çocuk boyutlarına dokunmaz) kullanır; gizlenen eleman (son bölümde Next) aradan kendiliğinden çıkar. Geniş butonlar `wideButton.png` oranında (560×161).

Popup'lar (`WinScreen`, `LoseScreen`, `QuitConfirmScreen`) `Show` içinde panelini ölçek 0.8 → 1 ve fade ile açar (`PopupTime`, `SetUpdate(true)`). Bu ortak animasyon `ScreenView`'daki `protected void PopIn(Transform panel, float time)` yardımcısındadır. Tam ekran yarı saydam arka plan tahtaya dokunmayı da engeller.

### 12.3 Ekranlar
| Ekran | İçerik | Event'ler | Not |
|---|---|---|---|
| `LevelMapScreen` | Aşağıdan yukarıya bölüm zinciri | `LevelSelected(int)` (sadece açık düğümler için) | `Fire` kullanmaz; §12.4 |
| `Hud` | Bölüm numarası + süre paneli + **baştan başlat** butonu | `RestartClicked` | `Fire` kullanmaz (tekrar tekrar basılabilir); tıkta `Feedback.UiClick()` |
| `WinScreen` | "Well done!", Next, Main Menu | `NextClicked`, `MenuClicked` | `Show(bool showNextButton)`; son bölümde "All levels completed!" |
| `LoseScreen` | "Out of space!", Try Again, Main Menu | `RetryClicked`, `MenuClicked` | |
| `QuitConfirmScreen` | "Quit the game?", Yes, No | `ConfirmClicked`, `CancelClicked` | Menüde overlay |

**HUD yerleşimi:**
```
┌──────────────────────────────────┐
│[⏱ 1:00]     Level 3      [Restart]│   ← SafeAreaFitter içinde
```
**Süre paneli:** süre metni `Timer.png` görselinin (`TimerPanel`, `Image`, raycast kapalı, oranı korunur) çocuğudur; metnin anchor'ları görseldeki koyu yuvaya (202x90 px içinde x 79..180, y 26..68) oturur, böylece süre hep o yuvanın içinde ortalanır. Süresiz bölümde `Hud` metni değil paneli gizler.
HUD'da başka bir şey **yok** (ayarlar, ana menü butonu, sayaç yok — bkz. §20).

### 12.4 `LevelMapScreen` ve `LevelNodeView`
- `ScrollRect` içinde dikey içerik; **ilk bölüm en altta.**
- **Düğümler bir kez kurulur:** `Build(int levelCount)` bootstrap'te bir kez çağrılır (düğümler ve çizgiler oluşturulur). `Show(IProgress progress)` her açılışta sadece düğüm durumlarını tazeler ve kaydırır; düğümleri yeniden oluşturmaz.
- **Düğüm yerleşimi:**
```csharp
private static readonly float[] Zigzag = { 0f, 150f, 0f, -150f };

float y = BottomPadding + index * NodeSpacing;     // NodeSpacing varsayılan 250
float x = Zigzag[index % Zigzag.Length];
// içerik yüksekliği = 2 * BottomPadding + (count - 1) * NodeSpacing
```
- **Bağlantı çizgisi** (ardışık iki düğüm arası, bir UI Image):
```csharp
Vector2 delta = to - from;
line.anchoredPosition = (from + to) / 2f;
line.sizeDelta = new Vector2(delta.magnitude, LineThickness);
line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
```
- **Açılışta kaydırma:**
```csharp
int currentIndex = FirstNotCompleted(progress, count);   // geçilmemiş ilk bölüm; hepsi geçildiyse count - 1
Canvas.ForceUpdateCanvases();                            // layout kurulmadan konum ayarı yok sayılır
scrollRect.verticalNormalizedPosition = count > 1 ? currentIndex / (float)(count - 1) : 0f;   // 0 = en alt
```
- **Düğüm durumları** (`LevelNodeView.SetState`):

| Durum | Koşul | Görünüm | Tıklanınca |
|---|---|---|---|
| `Completed` | `progress.IsCompleted(i)` | Renkli, onay işareti | Tekrar oynanır |
| `Current` | Açık ve geçilmemiş | Vurgulu, hafif nabız (DOTween Yoyo; `Show`'da başlar, `Hide`'da `DOKill`) | Oynanır |
| `Locked` | `!progress.IsUnlocked(i)` | Gri, kilit ikonu | Düğüm kendi içinde `DOShakePosition` ile sallanır; **`LevelSelected` yayınlanmaz** |

- `LevelNodeView` katmanları (her katmanın tek sahibi): kök `RectTransform` → kilitli sallanma (`DOShakeAnchorPos`); `Body` → "current" nabzı (ölçek). Durumlar `LevelNodeState { Completed, Current, Locked }`. `Body` görseli `Content/Images/UI/level1.png` (beyaz daire, gri kenar); durum rengi onu boyar. Düğüm ve bağlantı çizgisi prefab'ları `Content/Prefabs/UI/` altındadır (`LevelNode`, `MapLine`).
- 5 bölüm için düğümler doğrudan oluşturulur. Yüzlerce bölüm olursa sadece görünenleri çizen bir havuz eklenir (sadece `LevelMapScreen`'in içini etkiler).

### 12.5 `UiText` — 0/1 dönüşümü tek yerde
Kodda bölümler **0'dan**, oyuncunun gördüğü numaralar **1'den** başlar. Dönüşüm sadece burada:
```csharp
public static class UiText
{
    public static string LevelLabel(int index) => $"Level {index + 1}";
}
```
Harita düğümleri ve HUD bu metodu kullanır; başka hiçbir yerde `+ 1` yazılmaz.

### 12.6 `SafeAreaFitter`
Her ekranın kök paneline eklenir; `Screen.safeArea`'yı RectTransform anchor'larına çevirir. Çentikli/yuvarlak köşeli ekranlarda butonlar kesilmez. Editörde **Device Simulator** ile test edilir.

---

## 13. Ses ve Efektler

### 13.1 Zamanlama kuralı
Ses ve efektler **Core olaylarına değil, animasyon anlarına** bağlanır. Core her şeyi aynı milisaniyede sonuçlandırdığı için, olaylara doğrudan bağlanan sesler üst üste çalardı. Sesleri `GameplayPresenter` ve `BusLane` adımları, ilgili animasyon başladığında/bittiğinde `IFeedback` üzerinden tetikler.

### 13.2 `IFeedback`
```csharp
public interface IFeedback
{
    void PassengerTapped(Vector3 at);
    void PassengerBlocked(Vector3 at);
    void PassengerBoarded(int seat);
    void PassengerWaiting();
    void BusDeparted(Vector3 at);
    void BusArrived();
    void Won();
    void Lost();
    void UiClick();
}
```

### 13.3 Olay → ses/efekt tablosu
| Olay | Ne zaman? | Ses (`SoundId`) | Efekt (`VfxId`) |
|---|---|---|---|
| Geçerli dokunuş | Squash başlarken | `Pop` | — |
| Yolu kapalı yolcu | Sallanma başlarken | `Bonk` | — |
| Otobüse biniş | Zıplama bitince | `Board`, perde `1 + index * 0.1` (index = o otobüse binen sayısı − 1) | — |
| Koltuğa varış | Koltuk yürüyüşü bitince (`WalkAlong` `onArrived`) | `Seat` | — |
| Otobüs gidiyor | Gidiş başlarken | `Horn` | `ExhaustPuff` |
| Otobüs geliyor | Geliş bitince (`BusArriveStep`) | `Brake` | — |
| Kazanma | Popup açılırken (`WinState.Enter` → `feedback.Won()`) | `Win` | `Confetti` |
| Kaybetme | Popup açılırken (`LoseState.Enter` → `feedback.Lost()`) | `Lose` | — |
| UI butonu | Tıklanınca | `Click` | — |

Perde hesabı: otobüs dolarken "do-re-mi" hissi verir.
```csharp
float pitch = 1f + index * 0.1f;   // 1.0 → 1.1 → 1.2 (binişler sırasız olduğu için koltuk değil binen sırası)
```

### 13.4 Sınıflar
| Sınıf | Sorumluluğu |
|---|---|
| `FeedbackPlayer : IFeedback` | Hangi olayda hangi ses/efekt; `AudioPlayer` ve `VfxPlayer`'ı birleştirir |
| `AudioPlayer` | 6 adet `AudioSource`'tan oluşan havuz, sırayla (round-robin) kullanılır. Çalışma sırasında kaynak oluşturulmaz. **Aynı `SoundId` bir karede en fazla bir kez çalar** (bekleme alanından aynı anda binişlerde ses yığılmasın) |
| `VfxPlayer` | `VfxId` başına `ParticleSystem` havuzu. Her efekt prefab'ında `main.stopAction = ParticleSystemStopAction.Callback` ve küçük bir `PooledParticle` bileşeni var; Unity'nin `OnParticleSystemStopped` mesajında `VfxPlayer`'a geri döner (her karede yoklama yok) |
| `NullFeedback` | Hiçbir şey yapmayan `IFeedback`. Faz 3–5'te ve testlerde kullanılır |
| `SoundLibrary` (ScriptableObject) | `SoundId` → `AudioClip` + ses seviyesi. Ses değiştirmek kod gerektirmez |

`SoundId`: `Pop, Bonk, Board, Seat, Horn, Brake, Win, Lose, Click`
`VfxId`: `ExhaustPuff, Confetti`

- `PooledParticle` `[ExecuteAlways]` taşır: durma mesajı EditMode testlerinde de gelir; sahibi (`VfxPlayer`) yoksa (editörde prefab önizlemesi) mesaj yok sayılır.
- `Confetti` dünya merkezinde (tahtanın ortası, `Vector3.zero`) yukarı doğru patlar. Efekt prefab'ları `Content/Prefabs/Vfx/`, sesler `Content/Audio/`, `SoundLibrary` `Content/Settings/` altındadır. `AudioPlayer` `[Audio]` üzerinde, `VfxPlayer` onun `Vfx` alt nesnesindedir.

### 13.5 Ses import ayarları
| Tip | Load Type | Sıkıştırma | Diğer |
|---|---|---|---|
| Kısa efektler | Decompress On Load | ADPCM | Force To Mono |

Ses **her zaman açık**; oyuncu telefonun ses tuşları/sessiz moduyla kontrol eder (ayarlar menüsü yok).

### 13.6 Parçacık kuralları (mobil)
- Az parçacık (Confetti ≤ 60, ExhaustPuff ≤ 12), tek doku atlası, unlit materyal.
- Saydam üst üste çizimi (overdraw) sınırlı tut.

---

## 14. Composition Root ve Sahne Hiyerarşisi

### 14.1 Tek sahne: `Main.unity`
Menü ve oyun aynı sahnede. Sahne yükleme yok; geçişler anlık; havuzlar bölümler arasında yaşar.
- Build Settings'te ilk ve tek sahne `Main.unity`'dir. Editörde `MainSceneOnStartup` (Game.Editor, `[InitializeOnLoad]`) Unity açılınca `Main.unity`'yi açar (oturum başına bir kez) ve `EditorSceneManager.playModeStartScene`'i `Main.unity` yapar: hangi sahne açıkken Play'e basılırsa basılsın oyun `Main.unity`'den başlar.

```
Main
├── [Bootstrap]          GameBootstrap
├── [Camera]             Camera + CameraFitter
├── [Light]              Directional Light (gerçek zamanlı gölge KAPALI)
├── [World]
│   ├── BoardView
│   ├── WaitingAreaView
│   ├── GameplayPresenter
│   └── Pools            (havuzlanan nesnelerin ebeveyni)
├── [Input]              InputHandler, BackButtonInput
├── [Audio]              AudioPlayer (+ AudioSource'lar), VfxPlayer
├── [UI]
│   ├── MapCanvas        LevelMapScreen
│   ├── HudCanvas        Hud
│   ├── WinCanvas        WinScreen
│   ├── LoseCanvas       LoseScreen
│   └── QuitCanvas       QuitConfirmScreen
└── EventSystem          InputSystemUIInputModule
```

### 14.2 `AppDependencies` — durumların ortak bağımlılık paketi
`AppStates`'in constructor'ı uzun bir parametre listesine dönüşmesin diye bağımlılıklar tek bir sade sınıfta toplanır. Durumlar ihtiyaç duyduklarını buradan okur.
```csharp
public sealed class AppDependencies
{
    // Core / servisler
    public ILevelCatalog Catalog;
    public IProgress Progress;
    public IPathFinder PathFinder;
    public IFeedback Feedback;
    public LayoutSettings LayoutSettings;

    // Dünya
    public BoardView BoardView;
    public WaitingAreaView WaitingAreaView;
    public GameplayPresenter Presenter;
    public CameraFitter CameraFitter;

    // Input
    public InputHandler Input;
    public BackButtonInput BackButton;

    // UI
    public LevelMapScreen MapScreen;
    public Hud Hud;
    public WinScreen WinScreen;
    public LoseScreen LoseScreen;
    public QuitConfirmScreen QuitConfirmScreen;
}

public sealed class AppStates
{
    public AppStates(AppStateMachine machine, AppDependencies deps);
    // Menu(), Gameplay(int), Win(int), Lose(int)
}
```
Bu sınıf **bir veri paketidir, servis bulucu (service locator) değildir:** sadece `GameBootstrap` doldurur, sadece `AppStates` ve durumlar okur.

### 14.3 `GameBootstrap`
Sahnedeki **tek** "her şeyi bağlayan" yer. Sahne referanslarını ve ScriptableObject'leri Inspector'dan (`[SerializeField]`) alır, saf C# nesnelerini `new` ile kurar.

```csharp
private void Awake()
{
    Application.targetFrameRate = 60;
    DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
           .SetCapacity(200, 50);

    inputHandler.enabled = false;                     // sadece GameplayState açar

    // --- Editörden "Bu bölümü oyna" isteği var mı? (Faz 7)
    int requestedLevel = -1;
    bool launchedFromEditor = false;
#if UNITY_EDITOR
    launchedFromEditor = EditorLevelRequest.TryConsume(out requestedLevel);   // dönüş değeri esas alınır
#endif

    // --- Servisler
    var pathFinder = new BfsPathFinder();
    IProgressStorage storage = launchedFromEditor
        ? new InMemoryProgressStorage()               // editör denemesi gerçek kaydı bozmasın
        : new PlayerPrefsProgressStorage();           // Faz 4'te geçici olarak InMemoryProgressStorage
    var progress = new ProgressService(storage, levelCatalog.Count);
    IFeedback feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, soundLibrary);   // Faz 3–5: new NullFeedback()

    // --- View'lar ve ekranlar
    presenter.Init(colorPalette, animationSettings, layoutSettings, feedback);
    boardView.Init(layoutSettings);
    waitingAreaView.Init(layoutSettings);
    foreach (ScreenView screen in screens) screen.Init(feedback);
    mapScreen.Build(levelCatalog.Count);
    presenter.Prewarm(); boardView.Prewarm(); waitingAreaView.Prewarm();   // §10.2

    // --- Akış
    var deps = new AppDependencies
    {
        Catalog = levelCatalog, Progress = progress, PathFinder = pathFinder, Feedback = feedback,
        LayoutSettings = layoutSettings,
        BoardView = boardView, WaitingAreaView = waitingAreaView, Presenter = presenter, CameraFitter = cameraFitter,
        Input = inputHandler, BackButton = backButton,
        MapScreen = mapScreen, Hud = hud, WinScreen = winScreen, LoseScreen = loseScreen,
        QuitConfirmScreen = quitConfirmScreen,
    };
    var machine = new AppStateMachine();
    var states  = new AppStates(machine, deps);

    // Başlangıçta tüm ekranlar gizli
    foreach (ScreenView screen in screens) screen.Hide();

    machine.ChangeTo(launchedFromEditor ? states.Gameplay(requestedLevel) : states.Menu());
}
```
- Singleton yok, `FindObjectOfType` yok, DI framework yok.
- **Fazlara göre:** Faz 3'te bootstrap sadece dünya + input + presenter'ı kurar ve doğrudan 1. bölümü başlatır. Akış (Faz 4), kalıcı kayıt (Faz 5), `FeedbackPlayer` (Faz 6) ve `EditorLevelRequest` (Faz 7) satırları kendi fazlarında eklenir.
- `EditorLevelRequest` (Runtime, sadece `UNITY_EDITOR`) bölüm editöründeki "Bu bölümü oyna" isteğini okur; ayrıntı §15.3.

---

## 15. Editör Araçları

### 15.1 `LevelAssetEditor` (özel Inspector)
```
┌─ Level_Holes ─────────────────────────────────────┐
│ Size: [6] x [5]     Slots: [5]   Capacity: [3]    │
│                                                   │
│ Palette: [Floor][Wall][Void] ●R ●G ●B ●Y ●P ●O    │
│ ┌──┬──┬──┬──┬──┬──┐                               │
│ │R │R │B │B │G │G │   ← tıkla / sürükle ile boya   │
│ │Y │░░│R │G │░░│B │                               │
│ └──┴──┴──┴──┴──┴──┘                               │
│ Buses: G R B Y G R B   [Auto Fill]                │
│                                                   │
│ Level is valid. (ya da hata listesi)              │
│ [Solve]  → Solvable · 1,234 states · Random win rate 7% │
│ [Play This Level ▶]                               │
└───────────────────────────────────────────────────┘
```
- **Boyut değişimi** mevcut içeriği korur (büyürken yeni hücreler `.`, küçülürken kırpılır).
- **Canlı doğrulama:** Her değişiklikte `LevelParser` + `LevelValidator`; hatalar liste halinde.
- **Otomatik doldur:** Her renk için `yolcu sayısı / kapasite` kadar otobüs ekler (renk sırasıyla). Sıra tasarımcı tarafından elle düzenlenir. Bölünemiyorsa uyarı.
- **Çöz:** `LevelSolver` + rastgele oyuncu simülasyonu (1000 oyun). Sonuç ekranda.
- **Bu bölümü oyna:** §15.3.
- Tüm değişiklikler `Undo.RecordObject` ile geri alınabilir; `EditorUtility.SetDirty` ile kaydedilir. (Uygulamada `SerializedObject` üzerinden yapılır; bu ikisini kendisi sağlar.)
- Izgara ve otobüs mantığı IMGUI kodundan ayrı, test edilebilir `LevelEditing` (Game.Editor, statik) sınıfındadır: `Resize`, `Paint`, `AutoFillBuses`, `Check` (parse + doğrulama hataları). `LevelCatalogEditor` de durumu `Check` ile gösterir.

### 15.2 `LevelCatalogEditor`
- Liste + her bölüm için doğrulama durumu (✅/❌).
- Kalıcı uyarı kutusu: **"In a released game, only append levels at the end. Reordering shifts player progress."** (oyuncu ilerlemesi index tabanlı)

### 15.3 `EditorLevelLauncher` — "Bu bölümü oyna"
1. Seçili `LevelAsset`'in katalogdaki index'i bulunur (katalogda değilse uyarı).
2. `PlayerPrefs.SetInt("Editor.RequestedLevel", index)` yazılır ve Play moduna girilir.
3. `GameBootstrap` (sadece `UNITY_EDITOR` altında) bu anahtarı okur, siler ve doğrudan `Gameplay(index)` durumunu başlatır.
4. Bu şekilde başlatılan oturumda ilerleme **`InMemoryProgressStorage`** ile tutulur: editörde 4. bölümü denemek gerçek kayıtta 0–4 arasını açmaz (§14.3).

- Bölüm katalogda değilse uyarı penceresi açılır; aktif sahne `Main.unity` değilse kaydetme sorulur ve `Main.unity` açılır. İstenen index katalogdan büyükse (katalog değişmişse) menüden başlanır. `EditorLevelLauncher.FindIndex` ve `WriteRequest` testler için ayrı metotlardır.

> İki küçük sınıf var: **yazan** `EditorLevelLauncher` (`Game.Editor`) ve **okuyan** `EditorLevelRequest` (`Game.Runtime`, tamamı `#if UNITY_EDITOR` içinde, statik `TryConsume(out int index)`). Anahtar `PlayerPrefs` üzerinden taşınır çünkü `Game.Runtime` derlemesi `Game.Editor`'a referans veremez.

### 15.4 `DebugMenu` (Tools → Game)
Menü öğeleri **sadece Play modu dışında** aktiftir (`EditorApplication.isPlaying` iken pasif). Play modunda `ProgressService` veriyi bellekte tuttuğu için değişiklik yok sayılır ve sonraki kayıtta ezilir.
- **İlerlemeyi sıfırla:** `PlayerPrefs.DeleteKey(PlayerPrefsProgressStorage.DefaultKey)`
- **Tüm bölümleri aç:** `HighestCompletedIndex = Count - 2` yazar (son bölüm hariç hepsi geçilmiş, hepsi açık)
- Menü öğeleri "Tools/Game/Reset Progress" ve "Tools/Game/Unlock All Levels". İşi yapan metotlar anahtar alır (`ResetProgress(string key)`, `UnlockAllLevels(string key, int levelCount)`), böylece testler gerçek kayda dokunmadan `"progress_test"` ile çalışır.

---

## 16. Performans

### 16.1 Neden ECS yok?
~50 nesne var ve oyun mantığı sadece dokunuşta çalışıyor. ECS'nin kazancı her karede binlerce nesne güncellenirken başlar. Okunabilirlik kaybı kazancından büyük olurdu. Katmanlı mimari sayesinde ileride render tarafı gerekirse View katmanı tek başına değiştirilebilir; Core etkilenmez.

### 16.2 Render ayarları (URP 14)
| Ayar | Değer |
|---|---|
| SRP Batcher | **Açık** |
| HDR | Kapalı |
| MSAA | 2x (cihazda ölçülerek gerekirse kapatılır) |
| Ana ışık gölgesi | **Kapalı** — yerine karakter/otobüs altında **blob gölge** (yarı saydam yuvarlak quad) |
| Ek ışıklar | Kapalı |
| Post-processing | Yok |
| Materyaller | Renk başına bir URP Simple Lit materyal; `MaterialPropertyBlock` yok |
| Dokular | Tek atlas, 512–1024 px, ASTC |
| Karakter modeli | Low-poly, ~500–1500 üçgen; iskelet/Animator **yok** (animasyonlar prosedürel DOTween) |

### 16.3 Kod kuralları
- **"Update'te durum yoklama yok" kuralı:** Hiçbir sınıf her karede oyun durumunu kontrol ederek karar vermez; değişiklikler olay (event/callback) ile iletilir. Bilinçli istisnalar sadece dört tane: `InputHandler` ve `BackButtonInput` (input okumak), `GameplayPresenter` (`BusLane.Tick` + bitiş kontrolü), `SessionClock` (geçen süreyi Core'a vermek; zaman doğası gereği kare kare ilerler, Faz 9). Başka bir sınıfa `Update` eklenmez.
- **Her karede sıfır GC tahsisi.** Dokunuş başına küçük tahsisler (yol listesi, `OnComplete` lambda'ları) kabul.
- BFS tamponları yeniden kullanılır (§6.10).
- Oyun döngüsünde LINQ, string birleştirme, `GetComponent` yok.
- `Instantiate`/`Destroy` sadece havuz ilk dolarken.
- `Application.targetFrameRate = 60`.

### 16.4 Bütçe (düşük segment Android)
| Metrik | Hedef |
|---|---|
| FPS | Sabit 60 |
| Draw call | < 60 |
| Sahnedeki üçgen | < 100 bin |
| Oyun sırasında kare başına GC tahsisi | 0 bayt |
| Bellek | < 300 MB |

### 16.5 Ölçüm
Tahmin değil ölçüm: **gerçek cihazda** Unity Profiler (CPU, GC Alloc sütunu), Frame Debugger (draw call), Memory Profiler. `InputHandler.Update` beklenen değer < 0,01 ms.

**Cihazda ölçüm adımları:**
1. Telefonda Geliştirici Seçenekleri → USB hata ayıklama açık; telefonu USB ile bağla.
2. File → Build Settings → Android → Switch Platform (bir kez).
3. Tools → Game → **Configure Development Build** (Development Build + Autoconnect Profiler açar; derin profil kapalı).
4. Build And Run. Oyun telefonda açılınca Window → Analysis → **Profiler** otomatik bağlanır (bağlanmazsa üstteki hedef listesinden telefonu seç).
5. Menüden Bölüm 5'i aç ve ~30 saniye oyna (hızlı ardışık dokunuşlar dahil). Profiler'da:
   - CPU Usage: kare süresi ≤ 16,6 ms (60 FPS).
   - Hierarchy görünümü, **GC Alloc** sütunu: oyun sırasında dokunuş olmayan karelerde 0 B.
   - Rendering modülü: **Batches** < 60 (SRP Batcher açıkken aynı shader'lı çizimler tek partide toplanır).
   - Memory: toplam < 300 MB.
6. Bir kareyi incelemek için Window → Analysis → Frame Debugger (telefona bağlıyken).
7. Profiler'da oyun sırasında `Instantiate` görülürse ilgili havuzun ön ısıtma sayısı (`Main.unity` Inspector'ında) büyütülür.

Editörde otomatik denetlenenler (`PolishTests`): URP ayarları (§16.2), modellerin üçgen sınırı, en büyük bölümün < 100 bin üçgen olması, tüm opak dünya materyallerinin tek shader (Simple Lit) kullanması ve gölge atmaması, ön ısıtma sayılarının katalogdaki her bölümü karşılaması, `BusLane.Tick`'in kare başına 0 bayt ayırması.

### 16.6 Sonraki faz (opsiyonel)
`OnDemandRendering.renderFrameInterval` ile oyuncu boştayken kare hızını düşürmek (pil). İlk sürümde yok.

---

## 17. Test Stratejisi

Tüm testler **EditMode** (Unity Test Runner). Core testleri Unity sahnesi açmadan, milisaniyeler içinde çalışır.

### 17.1 Test yardımcıları
- **Metinden bölüm kurma:** Testler bölümü ızgara metniyle tanımlar.
```csharp
var session = TestSessions.From(
    rows:  new[] { "R.",
                   "R." },
    buses: "R",
    capacity: 2,
    slots: 5,
    listener: recorder);
```
`TestSessions.From` bölümü **doğrulamadan** kurar (kural testlerinde bilinçli olarak eksik/uyumsuz bölümler de kurulabilsin). `capacity` ve `slots` parametreleri zorunludur. Varsayılan olarak `Start()`'ı da çağırır (`autoStart: true`); `Start_RaisesBusArrivedForFirstBus` gibi testler `autoStart: false` verip `Start()`'ı kendisi çağırır. Bilinen çözüm tekrar oynatma testleri de `Start()` sonrası dokunur.
- **`RecordingGameListener`:** Gelen olayları sırasıyla bir listeye yazar (`"Exited 0"`, `"Boarded 0 bus0 seat0"` ...). Olay sırası testleri bununla yazılır.
- **`InMemoryProgressStorage`:** Core'daki gerçek sınıf (§8.2); testlerde de kullanılır.
- **`FakeLevelCatalog`:** Verilen `LevelData` listesiyle çalışan sahte katalog.

### 17.2 `Game.Core.Tests` — en az şu testler
**GridPos / Board**
- `IsExit_TopRowFloor_True`
- `IsExit_UnderWall_False`
- `IsExit_UnderVoidColumn_True` (üçgen tahta)
- `IsExit_UnderMiddleHole_False` (üstünde var olan hücre olan delik)
- `GetCell_OutOfBounds_ReturnsVoid`
- `RemovePassenger_MakesCellWalkable`

**BfsPathFinder**
- `PassengerOnExit_PathIsSingleCell`
- `BlockedByPassengers_NoPath`
- `RoutesAroundWall`
- `CannotPassThroughVoid`
- `PathStartsAtPassengerAndEndsAtExit`
- `ReachableFromExits_IgnorePassengers_FindsIsland` (ada tespiti)

**Bus / BusQueue / WaitingArea**
- `Bus_CanBoard_WrongColor_False`, `Bus_Add_WhenFull_Throws`, `Bus_Add_ReturnsSeatIndex`
- `BusQueue_Advance_PastLast_CurrentNull`
- `WaitingArea_TryAdd_UsesLowestFreeSlot`
- `WaitingArea_TryTake_LowestMatchingSlot_OthersDoNotShift`
- `WaitingArea_Full_TryAddFalse`

**GameSession (kurallar §2)**
- `Start_RaisesBusArrivedForFirstBus`
- `Tap_EmptyCell_NoEvents`
- `Tap_BlockedPassenger_RaisesBlocked_StateUnchanged`
- `Tap_MatchingColor_Boards`
- `Tap_NonMatching_GoesToLowestFreeSlot`
- `BusFull_Departs_NextArrives_WaitingPassengersBoard` (olay sırası birebir §6.8 örneği)
- `ChainBoarding_WaitingFillsNextBus_ThenNextArrives`
- `WaitingOneSlotLeft_TapMatchingPassenger_GameContinues` (§2.8)
- `LastFreeSlotTaken_Lost_ExitedWaitingLostOrder` (§2.8)
- `LastPassengerBoards_Won_RaisedOnce` (kazanma tek kez)
- `TapAfterOver_Ignored`
- `TappingFrontThenBack_BothLeave` (§2.10)
- `Restart_NewSessionFromSameLevelData_IndependentState`

**LevelParser / LevelValidator / LevelSolver**
- `Parse_UnknownChar_Error`, `Parse_RaggedRows_Error`, `Parse_BusesIgnoreSpaces`
- `Parse_PassengerIds_ReadingOrder`
- `Validate_ColorCountMismatch_Error`
- `Validate_NoExit_Error`
- `Validate_Island_Error`
- `Solve_TrivialLevel_Solvable`
- `Solve_UnsolvableLevel_NotSolvable` — tek sütun `R / G / B` (R önde), otobüsler `"BGR"`, kapasite 1, koltuk 1. Doğrulamayı geçer ama çözülemez (referans simülatörle doğrulandı). Aynı tahta `"RGB"` sırasıyla çözülebilir
- `Solve_KnownSolutions_Replay` — §21.1'deki her bölümün bilinen çözümü `GameSession` üzerinde oynatılır, sonuç `Won`

**ProgressService**
- `Fresh_OnlyFirstUnlocked`
- `MarkCompleted_UnlocksNext`
- `MarkCompleted_Replay_DoesNotDecreaseOrWrite`
- `Load_ClampsToLevelCount`
- `PlayerPrefsStorage_Corrupt_ReturnsDefault` (`Game.Editor.Tests`'te; **test anahtarı** `"progress_test"` ile kurulur, test sonunda silinir — gerçek kayda dokunmaz)

### 17.3 `Game.Editor.Tests` — katalog güvenlik ağı
- `Catalog_AllLevels_ParseAndValidate` — gerçek `LevelCatalog` asset'i yüklenir (`AssetDatabase`), her bölüm parse edilir ve doğrulanır.
- `Catalog_AllLevels_Solvable` — her bölüm `LevelSolver` ile çözülür; `HitLimit` olursa da başarısız.
- Bu testler **commit öncesi / CI'da** çalışır. Kırmızıysa bozuk bölüm oyuna giremez.

### 17.4 Elle test listesi (her faz sonunda)
- Editörde ve Device Simulator'da (çentikli bir iPhone + bir Android profili) oyna.
- Hızlı ardışık dokunuş, animasyon ortasında baştan başlat, son bölümü kazan, kaybet → tekrar dene, geri tuşu her ekranda.

---

## 18. Uygulama Fazları ve Kabul Kriterleri

Her faz **tek başına derlenir ve testleri yeşildir.** Bir faz bitmeden sonrakine geçilmez.

### Faz 0 — Proje iskeleti
- Paketler (§4.1), Player/Project ayarları (§4.2, §4.3), DOTween + "Create ASMDEF", TMP font asset (§4.4).
- Klasörler ve 5 asmdef (§5). `Game.Core`'da "No Engine References" işaretli.
- Boş `Main.unity` sahnesi ve hiyerarşi iskeleti (§14.1).
- **Kabul:** Proje hatasız derlenir; boş bir Core testi Test Runner'da yeşil; `Game.Core`'a `using UnityEngine;` eklenince derleme hatası alınır (sonra geri alınır).

### Faz 1 — Core kuralları
- §6'nın tamamı: `GridPos`, `CellType`, `PassengerColor`, `Passenger`, `Board`, `Bus`, `BusQueue`, `WaitingArea`, `IGameListener`, `NullGameListener`, `GameSession`, `IPathFinder`, `BfsPathFinder`.
- `GameSession` `LevelData` aldığı için bu fazda **`LevelData`, `PassengerSpawn`, `LevelParser`, `ParseResult`** de yazılır (§7.1–7.3'ün parser kısmı).
- İlerleme mantığı (saf C#): `SaveData`, `IProgress`, `IProgressStorage`, `ProgressService`, `InMemoryProgressStorage` (§8.1–8.2).
- Test yardımcıları (`TestSessions`, `RecordingGameListener`) ve §17.2'deki GridPos/Board/BFS/Bus/WaitingArea/GameSession/LevelParser/ProgressService testleri.
- **Kabul:** Tüm Core testleri yeşil. Core'da hiçbir Unity tipi yok.

### Faz 2 — Bölüm doğrulama, çözücü ve 5 bölüm
- `LevelValidator`, `LevelSolver` (+ `EstimateRandomWinRate`), `ILevelCatalog` (Core).
- `LevelAsset`, `LevelCatalog` (Runtime). §21.1'deki **5 bölüm** asset olarak oluşturulur, kataloğa eklenir.
- §17.2 validator/solver testleri + §17.3 katalog testleri.
- **Kabul:** 5 bölüm doğrulanır ve çözülür; bilinen çözüm tekrar oynatma testleri yeşil; çözülemeyen örnek bölüm çözülemez çıkar.

### Faz 3 — Oynanabilir tek bölüm (küplerle)
- `LayoutSettings`, `AnimationSettings`, `ColorPalette`; `BoardLayout`, `BoardView`, `WaitingAreaView`, `PassengerView`, `BusView` (yer tutucu küp/kapsül modeller, §10.6'daki katmanlı hiyerarşiyle); `GameplayPresenter`, `BusLane` ve adımlar; `InputHandler`; `CameraFitter`.
- **`IFeedback` arayüzü ve `NullFeedback`** bu fazda yazılır (presenter ve adımlar ona bağlı); gerçek `FeedbackPlayer` Faz 6'da.
- Geçici olarak `GameBootstrap` doğrudan 1. bölümü başlatır ve `PresentationFinished` sonucunu konsola yazar.
- **Kabul:** Editörde ve Device Simulator'da 1. bölüm baştan sona oynanır; hızlı ardışık dokunuşta yolcular paralel yürür, her yolcu otobüs kapısına varınca beklemeden biner (paralel biniş, §10.8); kapalı yolcu sallanır; son koltuk dolunca oyun kaybedilir ve son yolcu koltuğunda sallanır.

### Faz 4 — Akış ve UI
- `IAppState`, `AppStateMachine`, `AppDependencies`, `AppStates`, 4 durum; `ScreenView` ve tüm ekranlar; `SafeAreaFitter`, `UiText`; `BackButtonInput`; çıkış onayı overlay'i.
- `IProgress` geçici olarak `ProgressService(new InMemoryProgressStorage(), count)` ile bağlanır (ikisi de Faz 1'de hazır).
- **Kabul:** Menü → bölüm → kazan → sonraki → ... → son bölüm (Next butonu gizli, "All levels completed!") → menü; kaybet → tekrar dene; HUD'dan baştan başlat (animasyon ortasında dahil); geri tuşu her ekranda §11.5'e uygun; kilitli düğüm sallanır ve harita tıklanabilir kalır; HUD butonuna dokunmak tahtayı tetiklemez.

### Faz 5 — Kalıcı ilerleme
- `PlayerPrefsProgressStorage`; bootstrap'te `InMemoryProgressStorage` yerine geçer. `DebugMenu`.
- **Kabul:** Uygulama kapatılıp açılınca harita doğru durumda; tekrar oynama ilerlemeyi bozmaz; debug menüsü (Play modu dışında) çalışır; depolama testi gerçek kayda dokunmaz.

### Faz 6 — Ses ve efektler
- `FeedbackPlayer`, `AudioPlayer`, `VfxPlayer`, `PooledParticle`, `SoundLibrary`, `SoundId`, `VfxId`; bootstrap'te `NullFeedback` yerine geçer.
- Yer tutucu sesler kabul (ekip sonra değiştirir).
- **Kabul:** §13.3 tablosundaki her olay doğru anda duyulur/görülür; bekleme alanından toplu binişte ses yığılması yok.

### Faz 7 — Bölüm editörü
- `LevelAssetEditor`, `LevelCatalogEditor`, `EditorLevelLauncher` + `EditorLevelRequest`.
- **Kabul:** Yeni bir bölüm sadece editörle (metin yazmadan) oluşturulup doğrulanır, çözülür ve "Bu bölümü oyna" ile açılır; bu şekilde oynanan bölüm gerçek ilerlemeyi değiştirmez.

### Faz 8 — Cila ve performans
- Gerçek modeller/materyaller, blob gölgeler, popup animasyonları, harita görselleri; havuz ön ısıtma sayılarının cihazda ayarlanması.
- Cihazda profil (§16.5); bütçe tablosu (§16.4) karşılanır.
- **Kabul:** Düşük segment bir Android cihazda sabit 60 FPS, kare başına 0 GC, < 60 draw call.

---

### Faz 9 — Bölüm süresi
- Tasarım: `Docs/TIMER_DESIGN.md` (projeye uyarlanmış; §0 farkları listeler).
- Core: `LevelTimer`, `LoseReason`, `IGameListener` (`OnTimerStarted`, `OnTimeChanged(int)`, `OnLost(LoseReason)`), `GameSession.Tick`, `LevelData.TimeLimitSeconds` + parser/validator.
- Runtime: `SessionClock`, presenter olayları, `PresentationFinished(result, reason)`, `GameplayState` aç/kapa, HUD süre metni (tahsissiz etiketler, son 10 sn kırmızı + nabız), `LoseScreen.Show(reason)`, `UiText.FormatTime`, bölüm editöründe süre alanı.
- İçerik: bölüm süreleri 60 / 60 / 75 / 90 / 120 sn (oyun testinde ayarlanır).
- **Kabul:** `Docs/TIMER_DESIGN.md` §7.

---

## 19. Karar Kaydı

Bu kararlar tartışılarak alındı. **Kod yazarken "daha iyisi" diye değiştirilmemeli;** değişiklik gerekirse önce bu tablo güncellenir.

| # | Konu | Seçilen | Reddedilenler | Neden |
|---|---|---|---|---|
| 1 | Kuralların yeri | Katmanlı OOP, saf C# Core | Bileşen tabanlı (MonoBehaviour), MVP, Redux tarzı reducer, ECS | OOP/SOLID'i en net gösteren, test edilebilir, okunabilir yapı. MVP bu boyutta tören; reducer OOP beklentisini zayıflatır; ECS ~50 nesne için gereksiz ve okunabilirliği düşürür |
| 2 | Katman sınırı | asmdef + "No Engine References" | Klasör disiplini | Kural derleyici tarafından garanti edilir |
| 3 | Core → View haberleşmesi | Tek `IGameListener` arayüzü | Doğrudan çağrı, çok sayıda C# event, global event bus, ScriptableObject event'leri | Olayların tam listesi tek dosyada; sıra net; test kolay. Event bus ve SO event'leri akışı görünmez yapar |
| 4 | Bağımlılık bağlama | Elle composition root (`GameBootstrap`) | VContainer/Zenject, Singleton/Service Locator | Bu boyutta DI framework okunabilirliği düşürür; singleton gizli bağımlılık yaratır |
| 5 | Hamle temsili | Metot çağrısı (`Tap`) | Command pattern | Tek hamle türü var, undo/güçlendirici yok. Gerekirse Command eklenir |
| 6 | Zamanlama | Mantık anında, görsel sonra | Mantığın animasyonu beklemesi | Kurallar zamanlamadan bağımsız ve test edilebilir kalır |
| 7 | Hızlı dokunma | **Serbest**; yürüyüşler paralel | Animasyon sırasında input kilidi | Kilit oyunu yavaşlatır, kötü UX. Orijinal oyunda da yürüyüşler eşzamanlı |
| 8 | Animasyon sıralaması | Sadece otobüs olayları sıralı (`BusLane`) | Tek global kuyruk | Global kuyruk hızlı dokunmayı öldürür |
| 9 | Kesişen yürüyüş yolları | **Kabul** (önlem yok) | Görsel itme, yol rezervasyonu | Oyuncu fark etmez. Yol rezervasyonu mantığı görsele bağlar — kesinlikle yapılmaz |
| 10 | Kapı önünde/bekleme alanından binişte üst üste görünme | **Kabul** | — | Oyuncu açısından önemsiz |
| 11 | Tween | **DOTween** | Kendi tween sistemi | Ekip kararı; okunur isimlendirilmiş API |
| 12 | Boyut | 3D | 2D | Orijinal oyun 3D |
| 13 | Tahta şekli | Kare hücre + `Void` ile serbest şekil | Üçgen/altıgen hücre (topoloji soyutlaması) | Üçgen tahtalar ve delikler tek kavramla çözülür; matematik basit kalır |
| 14 | Çıkış kuralı | "Sütunda üstündeki her hücre Void ise çıkış" | "Sadece üst satır çıkış" | Düzensiz tahtalarda doğru çalışan tek basit kural |
| 15 | Bölüm verisi | ScriptableObject (JSON'a çevrilebilir yapı) | JSON birincil kaynak, ikisi birden | Uzaktan bölüm güncelleme şu an istenmiyor. `ILevelCatalog` sayesinde ileride tek sınıfla geçilir |
| 16 | Bölüm sırası | `LevelCatalog` listesi; "son bölüm" = `HasNext` | Dosya adı/numara | Tek bilgi kaynağı; kodda bölüm sayısı yazmaz |
| 17 | Akış | State pattern (4 durum) | Tek `GameFlowController`, yığın (push/pop) durum makinesi | Her ekran küçük bir sınıf; yeni ekran = yeni sınıf |
| 18 | Tekrar dene / baştan başlat | Aynı `LevelData`'dan **yeni** session | Mevcut session'ı sıfırlama | "Sıfırlamayı unuttuk" hataları yapısal olarak imkânsız |
| 19 | Sahne | Tek sahne | Menü ve oyun ayrı sahneler | Anlık geçiş, havuzlar yaşar |
| 20 | İlerleme | Tek sayı (`HighestCompletedIndex`), PlayerPrefs + JSON | JSON dosyası, bulut | Doğrusal zincir için yeterli; en basit. `IProgressStorage` ile değiştirilebilir |
| 21 | UI | UGUI + TMP | UI Toolkit | 2022.3'te runtime veri bağlama yok; animasyonlu oyun UI'ı için UGUI |
| 22 | Input | Input System, `Pointer`, `Update` içinde okuma | Eski Input Manager; event/callback tabanlı input | Mobil + editör tek kod; callback içinde `IsPointerOverGameObject` güvenilmez |
| 23 | Dokunuş algılama | Zemin düzlemine raycast + hücre dönüşümü | Collider + Physics.Raycast | Fizik gereksiz; tek bölme işlemi |
| 24 | HUD | Bölüm numarası + baştan başlat | Ayarlar, ana menü butonu, ilerleme çubuğu | Ekip kararı |
| 25 | Ayarlar menüsü | **Yok** | Ses aç/kapa popup'ı | Ekip kararı; ses telefonun kendi kontrolleriyle |
| 26 | Ses zamanlaması | Animasyon anlarına bağlı | Core olaylarına bağlı | Core anında çalıştığı için sesler üst üste binerdi |
| 27 | Titreşim | **Yok** | `Handheld.Vibrate` | Kaba titreşim + kapatma seçeneği yok |
| 28 | Kaybetme | **Son koltuk dolunca anında kayıp** (§2.8) | Koltuk yokken binemeyen yolcuya dokunmak | Faz 3 oyun testinde ekip kararıyla değişti: dolu koltuklarla oyunun sürmesi yanlış bulundu |
| 29 | Unity sürümü | **2022.3.19f1, sabit** | 2022.3.62f2+ | Ekip kararı. Not: Unity'nin CVE-2025-59489 güvenlik bildirimi 2022.3 için yamayı 2022.3.62f2'de yayımladı; risk bilinerek sabit tutuluyor |
| 30 | Sonsuz/ölçeklenen harita | 5 bölümde doğrudan düğüm | Sanallaştırılmış liste | Gerektiğinde sadece `LevelMapScreen` içi değişir |

### 19.1 Konuşmaya göre netleştirilen/değişen noktalar
- `ILevelLoader` → **`ILevelCatalog`** ile birleştirildi (§7.5).
- `AnimationQueue` → **`GameplayPresenter`** + **`BusLane`** (§10.5, §10.8).
- `Game.View` assembly'si → **`Game.Runtime`** (Flow, UI, Input, Feedback de içinde).
- İlk taslaktaki "animasyon sırasında input kilidi" **kaldırıldı** (#7).
- Ayarlar menüsü **kaldırıldı** (#25).
- Kazanma kontrolü özyinelemenin içinden **dokunuşun sonuna** taşındı (çift tetiklenme hatası).
- `IGameListener`'a `OnPassengerBlocked` ve `OnPassengerLeftWaitingArea` eklendi.
- Tahta görsellerinin temizliği `GameplayState.Exit`'ten **sonraki durumun `Enter`'ına** taşındı (popup arkasında tahta görünsün diye, §11.6).
- Kendi `Tweener`/`Ease` sınıfları **iptal** (DOTween).
- Görseller Core nesnelerine değil **Id'lere** bağlanır (`PassengerSpawn`, bus Id) — `Begin` session'dan önce çağrılabilsin diye.
- `PassengerView` yürüyüşleri **parça kuyruğuyla** yönetir (çalışan DOTween Sequence'e ekleme yapılamaz).
- Tween referansı saklanmaz; adımlar kendi `IsDone` bayrağını tutar (DOTween recycling).
- `GameplayState.Exit` içindeki `DOTween.KillAll()` kaldırıldı; her görsel havuza dönerken kendi tween'lerini öldürür.
- Editörden başlatılan bölümde ilerleme bellekte tutulur.
- `CameraFitter` varsayılan `margin` değeri 0.15 → **0.3** (Faz 3'te ölçüldü: 0.15 dikey ekranda alt köşeleri kırpıyordu, §9.4).
- `ScreenView.Init` butonları soyut `BindButtons()` ile bağlar (Awake EditMode testlerinde çalışmaz); popup açılışı ortak `PopIn` yardımcısında (§12.2). HUD'daki baştan başlat butonunun metni "Restart" (font'ta ↻ glifi yok).
- **Dil:** oyuncu metinleri ve editör araçlarının metinleri Türkçeden **İngilizceye** çevrildi (Faz 7 sonrası ekip kararı; §1, §4.4, §7.3, §12.3, §12.5). Testler İngilizce metinleri ve sahnede Türkçe karakter kalmadığını denetler.
- **Tahta ve koltuk zeminleri** (ekip kararı): tahtanın ve koltuk sırasının altında bitişik iki zemin (§9.2, §9.3, §10.4); kamera sığdırması zeminleri kapsar.
- **Oturan yolcular** (ekip kararı): otobüs doluluğu beyaz noktalar yerine tavandaki koltuklarda beliren yolcularla gösterilir; yolcular koltukta hazır bekler, binişle açılır (§10.1, §10.7). `BusIndicator` ve iki gösterge malzemesi kaldırıldı. Kapasite okunsun diye her koltukta koyu bir kutu var. Koltuk ve yolcu tek `BusSeat` prefab'ında; büyük kapasitede ikisi birlikte küçülür (önce ayrı prefab'lardı, küçülünce yolcunun sadece kafası kalıyordu).
- **Bölüm düğümü görseli** (ekip kararı): düğüm gövdesi `level1.png` (§12.4).
- **Yol** (ekip kararı): otobüs şeridinin altında ekranı yatayda kaplayan bir yol (§9.3, §10.4). Park noktasındaki durak tabelası denendi, sonra kaldırıldı.
- **Süre paneli** (ekip kararı): süre metni `Timer.png` görselinin koyu yuvasında durur; süresiz bölümde panelin tamamı gizlenir (§12.3).
- **Çıkış kenarları** (oyun testi, ekip kararı): çıkış hücrelerinin üst kenarında gri `EdgePiece` yok (§10.4).
- **Bölüm süresi** (Faz 9): `Docs/TIMER_DESIGN.md` uygulandı (§2.11, §16.3). Kayıp sebebi popup'a taşınır ("Out of space!" / "Time's up!").
- **Paralel biniş** (oyun testi, ekip kararı): binişler artık şeritte sırayla değil paralel oynar; otobüs park edince koltuktaki yolcuları aynı anda koşar, her yolcu kapıya varınca biner, otobüs son yolcu girince kalkar. Gösterge ve perde binen sayısına göre (§10.5, §10.7, §10.8, §13.3). Kurallar ve testler değişmedi.
- **Otobüs kuyruğu ve durak kuralı** (Faz 8 sonrası oyun testi, ekip kararı): sıradaki otobüs park noktasının hemen arkasında bekler ve bir kısmı görünür (oyuncu sıradaki rengi görür); öndeki gidince kısa bir mesafe ilerler. Kimse kapıda beklemez: otobüsü park etmemiş yolcu duraktaki boş koltuğa oturur ve oradan biner; ekranda koltuk yoksa hücresinde tutulur (§9.2, §9.3, §10.5, §10.7, §10.8). Kurallar ve mevcut testler değişmedi.
- Faz 8: yer tutucu low-poly yolcu/otobüs mesh'leri, harita düğümüne gölge + çizilmiş onay işareti ve kilit; cihaz ölçümü için Tools → Game → Configure Development Build ve §16.5 adımları. Cihaz kabulü (60 FPS, 0 GC, < 60 batch) ekip tarafından telefonda ölçülür.
- Bölüm editörünün mantığı ayrı `LevelEditing` sınıfında (Faz 7, test edilebilirlik için, §15.1).
- `PooledParticle` `[ExecuteAlways]` + sahipsiz durma mesajını yok sayma (Faz 6, EditMode testinde mesajın çalışabilmesi için, §13.4).
- **Kaybetme kuralı** (Faz 3 oyun testi, ekip kararı): bekleme alanının son koltuğu dolduğu anda kayıp (§2.6, §2.8, karar #28). `OverflowPoint` kaldırıldı; kaybeden yolcu koltuğunda sallanır (§10.5). Koltuk sayıları değişmedi; Bölüm 3–5'in bilinen çözümleri ve rastgele kazanma oranları yeni kurala göre güncellendi (§21.1).

---

## 20. Kapsam Dışı

Bilerek yapılmayanlar. Eklenmesi istenirse önce bu doküman güncellenir.
- Ayarlar menüsü, ses aç/kapa
- Titreşim (haptics)
- Arka plan müziği (istenirse `AudioPlayer`'a tek döngüsel kaynak)
- Geri al (undo), güçlendiriciler (power-up), coin/ekonomi, reklam, uygulama içi satın alma
- Yıldız/puan sistemi
- Bulut kaydı, uzaktan bölüm yükleme
- Bölüm içi durumun kaydedilmesi (uygulama kapanırsa bölüm baştan)
- Gizli yolcu, tünel gibi ek engel tipleri (eklemek: parser tablosuna bir satır + hücre davranışı)
- Üçgen/altıgen hücreli tahtalar
- Lokalizasyon (metinler İngilizce sabit; `UiText` tek toplama noktası)
- Analitik

---

## 21. Ekler: Bölümler ve Referans Simülatör

### 21.1 Bölümler
Tüm bölümler referans simülatörle (§21.2) **doğrulandı ve çözüldü**; 3000 rastgele oyunda 0 kilitlenme. "Rastgele kazanma oranı" bölümler arası zorluk karşılaştırması içindir (§7.4).

Koordinatlar `(x, y)`; `y = 0` üst satır.

**Bölüm 1 — `Level_01_FirstTap`** · 3×3 · 2 renk · 5 koltuk · *Öğrettiği: dokun, bin* · Rastgele kazanma: %100
```
Rows:   "RBR"
        "B.B"
        "R.."
Buses:  "RB"     Capacity: 3   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (0,1) (2,1) (0,2)
```

**Bölüm 2 — `Level_02_Walls`** · 4×4 · 3 renk · 5 koltuk · *Öğrettiği: yolu kapalı yolcular, duvarlar* · Rastgele kazanma: ~%59
```
Rows:   "GRRB"
        "R#.G"
        "BG#B"
        "...."
Buses:  "RGB"    Capacity: 3   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (3,0) (0,1) (3,1) (0,2) (1,2) (3,2)
```

**Bölüm 3 — `Level_03_TriangleDown`** · 5×4 ters üçgen · 4 renk · 5 koltuk · *Öğrettiği: void, bekleme alanını akıllı kullanmak* · Rastgele kazanma: ~%14
```
Rows:   "RGBGR"
        "YBRBY"
        "-YG.-"
        "--.--"
Buses:  "RBGY"   Capacity: 3   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (3,0) (4,0) (0,1) (2,1) (1,1) (3,1) (4,1) (1,2) (2,2)
```

**Bölüm 4 — `Level_04_Holes`** · 6×5 delikli · 4 renk · 5 koltuk · *Öğrettiği: delikler, aynı renkten birden fazla otobüs, otobüs sırasını düşünmek* · Rastgele kazanma: ~%1
```
Rows:   "RRBBGG"
        "Y-RG-B"
        "GYB..R"
        "-BR#G-"
        "-GYRB-"
Buses:  "GRBYGRB"   Capacity: 3   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (3,0) (4,0) (5,0) (3,1) (0,1) (2,1) (5,1) (0,2) (1,2) (2,2) (5,2) (2,3) (2,4) (1,3) (4,3) (1,4) (3,4) (4,4)
```

**Bölüm 5 — `Level_05_Peak`** · 6×6 sivri üçgen + duvarlar · 5 renk · **6 koltuk** · *Hepsi bir arada* · Rastgele kazanma: ~%0,1
```
Rows:   "--RG--"
        "-BYPG-"
        "RP#.#B"
        "GRBRBY"
        ".R-GBG"
        "YB.RPG"
Buses:  "GRPBYRGB"   Capacity: 3   Slots: 6
Bilinen çözüm: (2,0) (3,0) (1,1) (2,1) (3,1) (4,1) (0,2) (0,3) (1,2) (5,2) (3,3) (4,3) (4,4) (4,5) (1,3) (2,3) (5,3) (1,4) (3,4) (0,5) (5,4) (1,5) (3,5) (5,5)
```
> Bölüm 5'te koltuk sayısı bilinçli olarak 6: 5 koltukla rastgele kazanma oranı çok daha düşüktü. Koltuk sayısının bölüm başına bir zorluk ayarı olduğunu da gösterir. Oyun testinde zor gelirse ilk ayar budur.

**Bölüm 6 — `test`** · 3×10 · 2 renk · 3 koltuk · *Ekip test bölümü: kapasite 15 (tavan koltuklarının sığdırılması)*

**Uç durum bölümleri (7–16).** Her biri tek bir kuralı ya da sınırı ucuna kadar zorlar; hepsi havuz ön ısıtma sınırları içinde (≤ 40 yolcu, ≤ 64 hücre, ≤ 8 duvar, ≤ 8 koltuk). `CatalogTests` hepsinin geçerli ve çözülebilir olduğunu, `EdgeCaseLevelTests` her bölümün kendi uç durumunu koruduğunu denetler.

**Bölüm 7 — `Level_07_SingleTap`** · 1×1 · 1 renk · 1 koltuk · Süre: yok · *Uç durum: En küçük bölüm: 1×1, tek yolcu, kapasite 1, tek koltuk, süre yok* · Rastgele kazanma: ~%100
```
Rows:   "R"
Buses:  "R"   Capacity: 1   Slots: 1
Bilinen çözüm: (0,0)
```

**Bölüm 8 — `Level_08_OneSlot`** · 3×2 · 2 renk · 1 koltuk · Süre: yok · *Uç durum: Tek bekleme koltuğu: yanlış renge ilk dokunuş anında kaybettirir* · Rastgele kazanma: ~%22
```
Rows:   "RBR"
        "B.."
Buses:  "RB"   Capacity: 2   Slots: 1
Bilinen çözüm: (0,0) (2,0) (1,0) (0,1)
```

**Bölüm 9 — `Level_09_SameColorQueue`** · 3×2 · 1 renk · 3 koltuk · Süre: yok · *Uç durum: Aynı renkten 6 tek kişilik otobüs art arda: hızlı otobüs kuyruğu* · Rastgele kazanma: ~%100
```
Rows:   "RRR"
        "RRR"
Buses:  "RRRRRR"   Capacity: 1   Slots: 3
Bilinen çözüm: (0,0) (1,0) (2,0) (0,1) (1,1) (2,1)
```

**Bölüm 10 — `Level_10_AllColors`** · 6×2 · 6 renk · 5 koltuk · Süre: 60 sn · *Uç durum: 6 rengin hepsi, her renkten bir otobüs* · Rastgele kazanma: ~%3
```
Rows:   "RGBYPO"
        "OPYBGR"
Buses:  "RGBYPO"   Capacity: 2   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (3,0) (5,0) (5,1) (4,0) (4,1) (0,1) (3,1) (1,1) (2,1)
```

**Bölüm 11 — `Level_11_WideBoard`** · 9×3 · 3 renk · 6 koltuk · Süre: 90 sn · *Uç durum: En geniş tahta (9 sütun): kamera genişliğe göre sığdırır* · Rastgele kazanma: ~%5
```
Rows:   "RGBRGBRGB"
        "GBRGBRGBR"
        "BRG...BRG"
Buses:  "RGBRGB"   Capacity: 4   Slots: 6
Bilinen çözüm: (0,0) (1,0) (2,0) (3,0) (4,0) (5,0) (6,0) (7,0) (2,1) (8,0) (0,1) (1,1) (3,1) (4,1) (5,1) (6,1) (7,1) (8,1) (0,2) (1,2) (7,2) (2,2) (6,2) (8,2)
```

**Bölüm 12 — `Level_12_TallBoard`** · 3×10 · 3 renk · 5 koltuk · Süre: 90 sn · *Uç durum: En uzun tahta (10 satır): kamera derinliğe göre sığdırır, uzun kuyruklar* · Rastgele kazanma: ~%1
```
Rows:   "RGB"
        "GBR"
        "BRG"
        "RGB"
        "GBR"
        "BRG"
        "R.B"
        "G.R"
        "B.G"
        "..."
Buses:  "RGBRGB"   Capacity: 4   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (0,1) (2,1) (0,2) (1,2) (0,3) (1,1) (2,2) (1,3) (2,3) (0,4) (1,4) (2,4) (0,5) (1,5) (2,5) (0,6) (2,7) (2,6) (0,7) (0,8) (2,8)
```

**Bölüm 13 — `Level_13_Maze`** · 5×5 · 3 renk · 4 koltuk · Süre: yok · *Uç durum: 8 duvarla (havuz sınırı) uzun, kıvrımlı bir koridor* · Rastgele kazanma: ~%95
```
Rows:   "RB..."
        "###.#"
        "G...."
        ".####"
        "RGB.."
Buses:  "RGB"   Capacity: 2   Slots: 4
Bilinen çözüm: (0,0) (1,0) (0,2) (0,4) (1,4) (2,4)
```

**Bölüm 14 — `Level_14_NearlyFull`** · 4×2 · 3 renk · 3 koltuk · Süre: yok · *Uç durum: Bekleme alanının bir eksiği kadar park gerekir; bir fazlası kaybettirir* · Rastgele kazanma: ~%22
```
Rows:   "GGBB"
        "RR.."
Buses:  "RGB"   Capacity: 2   Slots: 3
Bilinen çözüm: (0,0) (1,0) (0,1) (1,1) (2,0) (3,0)
```

**Bölüm 15 — `Level_15_ShortTimer`** · 3×3 · 3 renk · 5 koltuk · Süre: 12 sn · *Uç durum: 12 saniye: son 10 saniye uyarısı hemen başlar* · Rastgele kazanma: ~%42
```
Rows:   "RGB"
        "GBR"
        "BRG"
Buses:  "RGB"   Capacity: 3   Slots: 5
Bilinen çözüm: (0,0) (1,0) (2,0) (0,1) (1,1) (2,1) (1,2) (0,2) (2,2)
```

**Bölüm 16 — `Level_16_DeepExits`** · 5×3 · 3 renk · 5 koltuk · Süre: 60 sn · *Uç durum: Üst satırın altında çıkışlar (üstleri void), farklı yükseklikte çıkışlar* · Rastgele kazanma: ~%30
```
Rows:   "R---R"
        "GBRBG"
        "BGRGB"
Buses:  "RGB"   Capacity: 4   Slots: 5
Bilinen çözüm: (0,0) (4,0) (0,1) (1,1) (2,1) (3,1) (4,1) (2,2) (0,2) (1,2) (3,2) (4,2)
```

### 21.2 Referans simülatör (`Docs/reference/bus_jam_sim.py`)
- §2'deki oynanış kurallarının Python uygulaması: ayrıştırma, çıkış/yol hesabı, dokunuş, otobüs zinciri, çözücü (DFS + ziyaret kümesi) ve rastgele oyuncu simülasyonu. Doğrulamanın sadece 4–6. kurallarını (renk sayıları, çıkış, erişilebilirlik) uygular; 1–3. kurallar (sürüm, kapasite/koltuk ≥ 1, en az bir yolcu/otobüs) sadece C# tarafında.
- `levels.py` §21.1'deki 5 bölümü ve bilinen çözümlerini (`KNOWN_SOLUTIONS`) içerir.
- **Kullanımı:** C# uygulamasında bir kural sorusu çıkarsa (örn. "bu durumda hangi olay sırası?") aynı senaryo simülatörde çalıştırılıp karşılaştırılır. **Kuralların tanımı §2'dir;** simülatör ile §2 çelişirse §2 geçerlidir ve simülatör düzeltilir.
```bash
cd Docs/reference
python3 -c "from bus_jam_sim import *; from levels import LEVELS; print(solve(LEVELS['L4']))"
```
