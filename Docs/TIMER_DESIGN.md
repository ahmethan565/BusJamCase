# Zamanlayıcı (Level Timer) — Tasarım Referansı

> **Projeye uyarlanmış kopya (Faz 9).** Aşağıdaki "Projeye uyarlama" bölümü, orijinal tasarımdan farkları listeler; §1'deki ilkeler aynen geçerlidir.
>
> **Bu doküman ne?** Mevcut projeye bölüm süresi eklemek için bir **referans tasarım**. Orijinal `ARCHITECTURE.md`'deki isimlere göre yazıldı. Proje o zamandan beri değişmiş olabilir: **sınıf/metot isimlerini mevcut koddaki karşılıklarına uyarla, ama §1'deki ilkeleri değiştirme.** Bir ilke mevcut kodla uyuşmuyorsa önce kullanıcıya sor.

---

## 0. Projeye uyarlama (Faz 9)

| Konu | Orijinal tasarım | Projede |
|---|---|---|
| Kayıp metinleri | "Yer kalmadı!" / "Süre doldu!" | Proje kuralı gereği İngilizce: **"Out of space!"** / **"Time's up!"** |
| `LevelParser.Parse` | yeni zorunlu parametre | Sona **isteğe bağlı** `float timeLimitSeconds = 0f`; mevcut çağrılar ve testler değişmez |
| Presenter → HUD | Presenter `hud.SetTime` çağırır | Presenter World katmanında, UI'yı tanımaz: `TimeChanged(int)` C# olayını yayınlar; `GameplayState` buna abone olup `Hud.SetTime`'a iletir (Flow → UI doğrudan metot, §12.1). `OnTimerStarted` görsel bir şey yapmaz: HUD tam süreyi zaten gösteriyor, ilk `TimeChanged` ile saymaya başlar |
| `PresentationFinished` | sebebi taşır | `Action<SessionResult, LoseReason>`; `GameplayState` → `states.Lose(n, reason)` → `LoseScreen.Show(reason)` |
| `SessionClock` yeri | — | `Runtime/Flow/SessionClock.cs`, sahnede `[Bootstrap]` üzerinde, kapalı; `AppDependencies.Clock` |
| Test listener'ı | NoSpace testleri `OnLost(NoSpace)` bekleyecek şekilde güncellenir | `RecordingGameListener` `NoSpace` kaybını yine `"Lost"`, süre kaybını `"Lost TimeUp"` olarak yazar; yeni olaylar `"TimerStarted"`, `"Time {s}"`. Böylece mevcut testlerin beklentileri **değişmez**, sadece yardımcı sınıf yeni arayüze uyar |
| Son 10 saniye nabzı | DOTween | `AnimationSettings`'e `TimerPulseScale` (1.15) ve `TimerPulseTime` (0.5) eklenir |

## 1. İlkeler (değişmez)

1. **Süre bir oyun kuralıdır** ("süre bitince kaybedersin"), bu yüzden **Core'da** yaşar. MonoBehaviour'da süre kuralı olmaz.
2. **Core saati okumaz.** Core'da `UnityEngine` yok, dolayısıyla `Time.deltaTime` da yok. Geçen süre Core'a **dışarıdan verilir** (`Tick(deltaSeconds)`). Testler bu sayede gerçek zaman beklemeden "30 saniye geçti" diyebilir.
3. **Mantık anında, görsel sonra** ilkesi korunur. Süre, animasyonlardan bağımsız olarak mantıkta akar. Kazanma/kaybetme kararı mantıkta verilir, popup yine animasyonlar bitince açılır.
4. **Sıfırlama yok, yeniden oluşturma var.** Baştan başlat / tekrar dene yeni bir session kurar; zamanlayıcı da onunla birlikte sıfırdan gelir.
5. **Olay tabanlı UI.** HUD her karede süre okumaz; Core sadece **ekrandaki tam saniye değiştiğinde** olay yayınlar.

---

## 2. Kural tanımı

- Her bölümün bir süresi var: `TimeLimitSeconds` (bölüm verisinde). **`0` = zamansız bölüm.**
- Bölüm açıldığında süre **durur**. HUD tam süreyi donuk olarak gösterir.
- Süre, oyuncunun **tahtaya ilk dokunuşunda** başlar. Boş hücre, tahta dışı ya da yolu kapalı bir yolcuya dokunmak da sayılır. **UI'ya dokunmak sayılmaz** (ör. HUD'daki baştan başlat butonu).
- Süre sıfıra inerse bölüm **kaybedilir**, sebep: `TimeUp`.
- Bölüm mantıkta kazanıldığı/kaybedildiği anda süre **durur**.

---

## 3. Core

### 3.1 `LevelTimer` (yeni, tek sorumluluk)
```csharp
public sealed class LevelTimer
{
    private readonly float limit;

    public LevelTimer(float limitSeconds)
    {
        limit = limitSeconds;
        Remaining = limitSeconds;
    }

    public bool HasLimit => limit > 0f;
    public bool IsRunning { get; private set; }
    public float Remaining { get; private set; }
    public int RemainingWholeSeconds => (int)MathF.Ceiling(Remaining);   // ekranda gösterilen değer

    public bool StartIfNotRunning()            // bu çağrıda başladıysa true
    {
        if (!HasLimit || IsRunning || Remaining <= 0f) return false;
        IsRunning = true;
        return true;
    }

    public void Stop() => IsRunning = false;

    public bool Tick(float deltaSeconds)       // süre BU tick'te bittiyse true
    {
        if (!IsRunning) return false;
        Remaining = MathF.Max(0f, Remaining - deltaSeconds);
        if (Remaining > 0f) return false;
        IsRunning = false;
        return true;
    }
}
```

### 3.2 `LoseReason` (yeni)
```csharp
public enum LoseReason { NoSpace, TimeUp }
```

### 3.3 `IGameListener` değişiklikleri
```csharp
void OnTimerStarted();
void OnTimeChanged(int secondsLeft);   // SADECE tam saniye değişince (saniyede bir)
void OnLost(LoseReason reason);        // eskiden parametresizdi
```
Bu değişiklik arayüzü uygulayan **her sınıfı** derleme hatasına düşürür (presenter, `NullGameListener`, test listener'ları). Bu beklenen ve istenen bir durum: güncellenmesi gereken her yer derleyici tarafından gösterilir.

### 3.4 `GameSession` değişiklikleri
```csharp
private readonly LevelTimer timer;   // constructor'da: new LevelTimer(level.TimeLimitSeconds)

public float RemainingTime => timer.Remaining;   // testler için okuma amaçlı

public void Tap(GridPos cell)
{
    if (!started) throw new InvalidOperationException("Call Start() before Tap().");
    if (IsOver) return;

    if (timer.StartIfNotRunning())
        listener.OnTimerStarted();

    // ... mevcut dokunuş mantığı aynen ...
    // Mevcut "yer kalmadı" kaybı artık: Finish(SessionResult.Lost, LoseReason.NoSpace)
}

public void Tick(float deltaSeconds)
{
    if (IsOver || !timer.IsRunning) return;

    int before = timer.RemainingWholeSeconds;
    bool expired = timer.Tick(deltaSeconds);

    if (timer.RemainingWholeSeconds != before)
        listener.OnTimeChanged(timer.RemainingWholeSeconds);

    if (expired)
        Finish(SessionResult.Lost, LoseReason.TimeUp);
}

private void Finish(SessionResult result, LoseReason reason = LoseReason.NoSpace)
{
    Result = result;
    timer.Stop();
    if (result == SessionResult.Won) listener.OnWon();
    else listener.OnLost(reason);
}
```
- `Clone()` (çözücü için) zamanlayıcıyı da kopyalar. Çözücü `Tick` çağırmadığı için süre çözücüyü **hiç etkilemez**; çözülebilirlik süreden bağımsızdır.
- `StateKey`'e süre **eklenmez**.

### 3.5 Bölüm verisi
| Yer | Değişiklik |
|---|---|
| `LevelData` | `float TimeLimitSeconds` |
| `LevelAsset` | `[SerializeField] private float timeLimitSeconds = 0f;` |
| `LevelParser.Parse` | yeni parametre `timeLimitSeconds` |
| `LevelValidator` | yeni kural: `TimeLimitSeconds ≥ 0` |

**Geriye uyumluluk:** Varsayılan `0` olduğu için mevcut bölüm asset'leri Unity'de açılınca bu alanı `0` okur ve **zamansız** olarak eskisi gibi çalışır. Bölüm formatı sürümünü yükseltmek gerekmez.

---

## 4. Runtime

### 4.1 `SessionClock` (yeni MonoBehaviour)
```csharp
public sealed class SessionClock : MonoBehaviour
{
    private GameSession session;

    public void Bind(GameSession session) => this.session = session;
    public void Unbind() => session = null;

    private void Update() => session?.Tick(Time.deltaTime);
}
```
- Sahnede **işaretsiz (disabled)** durur; bootstrap da `enabled = false` yapar.
- `GameplayState.Enter`: `sessionClock.Bind(session)`, `sessionClock.enabled = true` (input'u açtığı yerde).
- `GameplayState.Exit`: `sessionClock.enabled = false`, `sessionClock.Unbind()`.
- Bootstrap'teki bağımlılık paketine (`AppDependencies` ya da mevcut karşılığı) eklenir.
- "Update kullanabilen sınıflar" istisna listesine eklenir. Gerekçe: zaman doğası gereği kare kare ilerler.
- `GameplayPresenter`'a **eklenmez**: o View; kurallara zaman vermek onun sorumluluğu değil.

**Uygulama arka plana alınırsa:** `Update` çalışmaz, süre durur. Unity dönüşteki ilk `deltaTime`'ı sınırladığı için (Maximum Allowed Timestep) ani bir sıçrama olmaz. Oyuncu açısından adil davranış; ek kod gerekmez.

### 4.2 Presenter
`GameplayPresenter` (ya da `IGameListener`'ı uygulayan mevcut sınıf) yeni olayları HUD'a iletir:
| Olay | Ne yapılır |
|---|---|
| `OnTimerStarted()` | `hud.SetTimerRunning(true)` (isteğe bağlı küçük bir vurgu) |
| `OnTimeChanged(s)` | `hud.SetTime(s)` |
| `OnLost(reason)` | `pendingResult = Lost`, `pendingLoseReason = reason` (mevcut kayıp görseli `NoSpace` için aynen) |

`PresentationFinished` olayı kaybetme sebebini de taşır (ör. `PresentationFinished(SessionResult, LoseReason)`), böylece `LoseState` ekrana doğru metni verir. `Clear()` `pendingLoseReason`'ı da sıfırlar.

### 4.3 UI
| Yer | Değişiklik |
|---|---|
| `Hud` | Süre metni (TMP). `SetTimeLimit(int seconds)` bölüm başında: tam süreyi donuk gösterir; zamansız bölümde metni gizler. `SetTime(int seconds)`: metni günceller. Son 10 saniyede metin kırmızı ve hafif nabız (DOTween, `Hide`'da `DOKill`) |
| `LoseScreen` | `Show(LoseReason reason)`: `NoSpace` → "Out of space!", `TimeUp` → "Time's up!" |
| `UiText` | `FormatTime(int seconds)` → `"1:05"` (biçimlendirme tek yerde) |
| Bölüm editörü | `TimeLimitSeconds` alanı |

**Tahsissiz metin (önerilen):** `FormatTime` her çağrıda yeni bir string üretir (saniyede bir, ~30 bayt). Sıfır tahsis için HUD, `SetTimeLimit` sırasında `0..limit` için tüm etiketleri bir kez üretip dizide tutar; `SetTime(s)` sadece `labels[s]` atar. 120 saniyelik bir bölüm için 121 kısa string, bölüm başına bir kez.

---

## 5. Kenar durumlar

| Durum | Davranış |
|---|---|
| Son dokunuşla mantıkta kazanıldı, animasyonlar sürerken süre "bitecekti" | **Kazanılır.** Karar dokunuş anında verildi, `Finish` zamanlayıcıyı durdurdu |
| Süre bitti, yolcular hâlâ yürüyor | Mantıkta anında kayıp (`TimeUp`). Popup mevcut kuralla animasyonlar bitince açılır |
| Süre bittikten sonra dokunuş | `IsOver` olduğu için yok sayılır |
| Aynı karede hem son dokunuş hem süre bitimi | Hangisi önce çalışırsa o geçerli; ikinci çağrı `IsOver` nedeniyle yok sayılır. Kayıp/kazanç iki kez tetiklenmez |
| Zamansız bölüm (`0`) | Zamanlayıcı hiç başlamaz, `OnTimerStarted`/`OnTimeChanged` hiç gelmez, HUD'da süre gizli |
| Baştan başlat / tekrar dene | Yeni session → yeni zamanlayıcı → yine ilk dokunuşu bekler |
| Animasyonlar sırasında süre | Akmaya devam eder. Hızlı dokunma serbest olduğu için oyuncu beklemeye zorlanmıyor, adil |

---

## 6. Testler (Core, EditMode)

- `Tick_BeforeFirstTap_DoesNotDecreaseTime`
- `FirstTap_EmptyCell_StartsTimer_RaisesTimerStartedOnce`
- `FirstTap_BlockedPassenger_StartsTimer`
- `Tick_RaisesTimeChanged_OnlyOnWholeSecondChange` (ör. 0,3 + 0,3 + 0,5 sn tick'lerinde tek olay)
- `TimeRunsOut_LostWithTimeUp_RaisedOnce`
- `Win_StopsTimer_LaterTickDoesNotLose`
- `TapAfterTimeUp_Ignored`
- `NoLimitLevel_NeverLosesByTime_NoTimerEvents`
- `Restart_NewSession_TimerWaitsForFirstTapAgain`
- Mevcut `NoSpace` kayıp testleri `OnLost(LoseReason.NoSpace)` bekleyecek şekilde güncellenir.
- Mevcut bilinen çözüm tekrar oynatma testleri ve çözücü testleri **değişmeden** geçmeli (`Tick` çağrılmadığı için).

---

## 7. Uygulama adımları (Faz 9)

Başlamadan önce mevcut durumu **commit'le**. Her adımdan sonra derleme temiz, testler yeşil.

1. **Core:** `LevelTimer`, `LoseReason`, `IGameListener` değişiklikleri, `GameSession` değişiklikleri, `LevelData`/parser/validator alanı. Derleyicinin gösterdiği tüm `IGameListener` uygulayıcıları güncellenir. §6 testleri.
2. **Runtime:** `SessionClock`, bootstrap bağlantısı, `GameplayState` aç/kapa, presenter'ın yeni olayları iletmesi, `PresentationFinished`'ın sebebi taşıması.
3. **UI:** HUD süre metni (tahsissiz etiketler), son 10 saniye vurgusu, `LoseScreen` sebebe göre metin, `UiText.FormatTime`. Editörde yapılacak sahne/prefab adımları kullanıcıya tarif edilir.
4. **İçerik:** Bölüm sürelerinin girilmesi (başlangıç önerisi aşağıda), bölüm editörüne alan eklenmesi.

**Kabul kriterleri:**
- Bölüm açıldığında süre donuk; ilk tahta dokunuşunda akmaya başlıyor; HUD butonuna dokunmak başlatmıyor.
- Süre bitince "Time's up!" popup'ı animasyonlar bittikten sonra açılıyor; yer kalmadığında "Out of space!".
- Son saniyede kazanılan bölüm kazanılmış sayılıyor.
- Zamansız bölümde süre görünmüyor.
- Profiler'da `SessionClock.Update` ihmal edilebilir düzeyde; oyun sırasında kare başına 0 GC (§8).

**Başlangıç süre önerisi** (bilinen çözümlerdeki dokunuş sayısına göre; oyun testinde ayarlanır):

| Bölüm | Dokunuş sayısı | Süre |
|---|---|---|
| 1 | 6 | 60 sn |
| 2 | 9 | 60 sn |
| 3 | 12 | 75 sn |
| 4 | 21 | 90 sn |
| 5 | 24 | 120 sn |

---

## 8. Performans maliyeti

**Sonuç: ihmal edilebilir.** Ayrıntı:

| Maliyet kalemi | Sıklık | Maliyet |
|---|---|---|
| `SessionClock.Update` çağrısı | Her kare | Tek bir motor → C# çağrısı. Sorun ancak yüzlerce/binlerce `Update`'li bileşende başlar; projede 4 tane var |
| `Tick` içindeki hesap | Her kare | Bir çıkarma, bir `Max`, bir `Ceiling`, iki karşılaştırma. Nanosaniyeler |
| `OnTimeChanged` olayı | Saniyede 1 | Bir arayüz çağrısı |
| HUD metin güncellemesi | Saniyede 1 | Tek bir TMP metninin mesh'i yeniden kurulur. HUD'un kendi Canvas'ı olduğu için diğer UI etkilenmez |
| String üretimi | Saniyede 1 → **0** | §4.3'teki önceden üretilmiş etiketlerle tahsis sıfır |
| Son 10 saniye nabız tween'i | Bölüm başına bir kez | DOTween recycling ile ihmal edilebilir |

Beklenen ölçüm: `SessionClock.Update` < 0,01 ms, GC Alloc sütunu 0.

### Alternatifler (neden seçilmedi)

| Alternatif | Nasıl? | Neden değil? |
|---|---|---|
| Her karede HUD'un `session.RemainingTime` okuması | HUD'da `Update` | "Update'te durum yoklama yok" kuralını bozar; kazancı yok |
| `DOTween.DelayedCall(limit, ...)` / `Invoke` ile tek seferlik süre bitimi | Zamanlayıcı kütüphanede | Süre kuralı Core'dan çıkıp View'a/kütüphaneye kayar; test için gerçek zaman beklemek gerekir; ekrandaki geri sayım için yine ayrı bir güncelleme gerekir |
| Coroutine + `WaitForSeconds(1)` | Saniyede bir döngü | Projede coroutine yasak; tahsis yapar; saniye sınırına kayar |
| Mutlak zaman damgası (`Tap(cell, now)`, `Tick(now)`) | Core başlangıç anını saklar | Delta yaklaşımıyla aynı maliyet, ama Core'a "şimdi" kavramı girer ve arka plana alma durumu ayrıca ele alınmalı. Delta daha sade |

Seçilen yaklaşım (Core'da `LevelTimer` + dışarıdan `Tick(deltaTime)`) hem en ucuz hem en test edilebilir olanı; ek optimizasyon gerekmiyor.
