# CLAUDE.md

Bu proje, Bus Jam benzeri 3D bir mobil bulmaca oyunu (Unity 2022.3.19f1, C#).

## Tek doğru kaynak
**`Docs/ARCHITECTURE.md`** — her işe başlamadan önce ilgili bölümünü oku. Oyun kuralları §2'de kesin olarak tanımlı; tüm kararlar ve gerekçeleri §19'da.
Bir karar kodda uygulanamıyor ya da çelişki görüyorsan **kendi başına değiştirme**; durup kullanıcıya sor. Değişiklik onaylanırsa önce dokümanı, sonra kodu güncelle.

## Çalışma şekli
- İş **fazlar halinde** ilerler (ARCHITECTURE.md §18). Bir faz bitmeden ve kabul kriterleri sağlanmadan sonrakine geçme.
- Her faz sonunda: derleme hatasız, tüm EditMode testleri yeşil, kabul kriterleri tek tek kontrol edilmiş.
- Unity Editor'ü sen açamazsın: sahne/prefab/asset kurulumu gereken adımları **kullanıcıya adım adım** tarif et (hangi GameObject, hangi bileşen, hangi alan). Mümkün olan her şeyi kodla (ör. editor script, `[CreateAssetMenu]`) yap.
- Sürüm sabit: Unity **2022.3.19f1**. Yükseltme önerme.

## Kırmızı çizgiler
- `Game.Core` asmdef'inde **No Engine References** açık kalır; Core'da `UnityEngine` yok.
- MonoBehaviour içinde oyun kuralı yok. Kurallar sadece `GameSession` ve Core sınıflarında.
- Singleton, Service Locator, `FindObjectOfType`, DI framework yok. Bağlama sadece `GameBootstrap`'te.
- Mantık animasyonu asla beklemez. Animasyon sırasında input kilidi yok (sadece popup'larda kapalı).
- Yol rezervasyonu / mantığı görsel zamanlamaya bağlayan hiçbir mekanizma yok.
- Yeni arayüz ekleme: izinli arayüz listesi ARCHITECTURE.md §3.4.
- Tween'ler DOTween, coroutine yok. **Tween referansı saklama** (recycling açık); bitişi `OnComplete` ile kendi bayrağına yaz. Havuza dönen görsel `ResetForPool` içinde kendi transform'larını `DOKill()` eder; dünya nesnelerinde en dıştaki tween/Sequence'e `SetLink(gameObject, LinkBehaviour.KillOnDisable)`. `DOTween.KillAll()` kullanma. (ARCHITECTURE.md §10.1)
- `PassengerView` katmanlarının her birinin tek sahibi var (§10.6); aynı özelliğe iki animasyon yazma.
- `record` / `init` kullanma (Unity 2022.3).
- Bölüm sayısı kodda yazmaz; "son bölüm" = `ILevelCatalog.HasNext`.
- 0 → 1 bölüm numarası dönüşümü sadece `UiText.LevelLabel`.
- Inspector `OnClick` listeleri kullanılmaz; butonlar kodda bağlanır.
- Kapsam dışı özellikler (ARCHITECTURE.md §20) eklenmez.

## Kod stili
- Tanımlayıcılar, yorumlar, oyuncu metinleri ve editör araçlarının metinleri İngilizce. Projede Türkçe metin yok (sadece dokümanlar Türkçe).
- Her sınıfın tek sorumluluğu var; okunabilirlik performans mikro-optimizasyonlarından önce gelir (bütçe §16.4 içinde kaldığı sürece).
- Oyun döngüsünde (her kare) GC tahsisi yok: LINQ, string birleştirme, `GetComponent` yok.

## Kural sorusu çıkarsa
`Docs/reference/bus_jam_sim.py` §2'nin referans uygulamasıdır. Bir senaryonun beklenen sonucunu orada çalıştırarak doğrula. Çelişkide §2 geçerlidir.
