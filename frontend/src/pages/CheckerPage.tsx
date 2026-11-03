import { useState } from "react";
import type { FormEvent } from "react";
import QrScanner from "@/components/QrScanner";
import { checkerApi } from "@/api/ticketsApi";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { TicketValidationResult } from "@/types";

type FlashState = { kind: "success" | "error"; title: string; detail: string } | null;

/**
 * Кабінет контролера (Етап 7, доопрацьовано за аудитом п.3): два режими сканування.
 *
 * Режим камери — "один скан = одна дія": QR розпізнано → одразу викликаємо /tickets/validate
 * і показуємо ПОВНОЕКРАННИЙ зелений/червоний індикатор (як і очікується від ТЗ для живого
 * показу на екзамені), після паузи сканер автоматично реактивується для наступного гостя.
 *
 * Ручний режим — той самий двокроковий флоу, що й раніше (превʼю → підтвердження), корисний
 * коли камера недоступна або QR пошкоджений і код потрібно ввести вручну/виправити помилку.
 */
export default function CheckerPage() {
  const [mode, setMode] = useState<"camera" | "manual">("camera");
  const [scannerActive, setScannerActive] = useState(true);
  const [flash, setFlash] = useState<FlashState>(null);

  // --- Ручний режим (текстове поле) ---
  const [code, setCode] = useState("");
  const [preview, setPreview] = useState<Awaited<ReturnType<typeof checkerApi.getTicketByCode>> | null>(null);
  const [manualError, setManualError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleCameraDetected = async (scannedCode: string) => {
    setScannerActive(false);
    try {
      const result: TicketValidationResult = await checkerApi.validateTicket(scannedCode);
      setFlash({ kind: "success", title: "Пропущено", detail: result.seatLabel });
    } catch (err) {
      setFlash({ kind: "error", title: "Відмовлено", detail: extractApiErrorMessage(err, "Квиток недійсний.") });
    } finally {
      // Пауза, щоб контролер встиг прочитати результат, перш ніж сканер знову оживе.
      setTimeout(() => {
        setFlash(null);
        setScannerActive(true);
      }, 2200);
    }
  };

  const handleLookup = async (e: FormEvent) => {
    e.preventDefault();
    if (!code.trim()) return;
    setLoading(true);
    setManualError(null);
    setPreview(null);
    try {
      const result = await checkerApi.getTicketByCode(code.trim());
      setPreview(result);
    } catch (err) {
      setManualError(extractApiErrorMessage(err, "Квиток не знайдено."));
    } finally {
      setLoading(false);
    }
  };

  const handleConfirm = async () => {
    if (!preview) return;
    setLoading(true);
    setManualError(null);
    try {
      const result = await checkerApi.validateTicket(preview.ticketCode);
      setFlash({ kind: "success", title: "Пропущено", detail: result.seatLabel });
      setPreview(null);
      setCode("");
      setTimeout(() => setFlash(null), 2200);
    } catch (err) {
      setManualError(extractApiErrorMessage(err, "Не вдалося підтвердити прохід."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-md px-6 py-12">
      {/* Повноекранний зелений/червоний індикатор поверх усього іншого UI */}
      {flash && (
        <div
          className={`fixed inset-0 z-50 flex flex-col items-center justify-center gap-3 ${
            flash.kind === "success" ? "bg-green-600" : "bg-accent"
          }`}
        >
          <p className="marquee-heading text-6xl text-white">{flash.kind === "success" ? "✓" : "✕"}</p>
          <p className="marquee-heading text-3xl text-white">{flash.title}</p>
          <p className="text-white/90">{flash.detail}</p>
        </div>
      )}

      <h1 className="marquee-heading text-3xl">Кабінет контролера</h1>

      <div className="mt-4 flex gap-2">
        <button
          onClick={() => setMode("camera")}
          className={`rounded-sm px-3 py-1.5 text-sm ${mode === "camera" ? "bg-marquee text-charcoal-950" : "border border-charcoal-600 text-ink-muted"}`}
        >
          Камера
        </button>
        <button
          onClick={() => setMode("manual")}
          className={`rounded-sm px-3 py-1.5 text-sm ${mode === "manual" ? "bg-marquee text-charcoal-950" : "border border-charcoal-600 text-ink-muted"}`}
        >
          Ввести код вручну
        </button>
      </div>

      {mode === "camera" ? (
        <div className="mt-6">
          <QrScanner active={scannerActive} onDetected={handleCameraDetected} />
          <p className="mt-3 text-center text-xs text-ink-muted">Наведіть камеру на QR-код квитка</p>
        </div>
      ) : (
        <div className="mt-6">
          <form onSubmit={handleLookup} className="flex gap-2">
            <input
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="CV-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
              className="flex-1 rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 font-mono text-sm outline-none focus:border-marquee"
            />
            <button type="submit" disabled={loading} className="rounded-sm bg-accent px-4 py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
              Знайти
            </button>
          </form>

          {manualError && <p className="mt-4 text-sm text-accent">{manualError}</p>}

          {preview && (
            <div className="mt-6 rounded-sm border border-charcoal-700 p-5">
              <p className="text-xs uppercase tracking-wide text-ink-muted">
                {preview.status === "ACTIVE" ? "Дійсний" : preview.status === "USED" ? "Уже використано" : "Скасовано"}
              </p>
              <h2 className="marquee-heading mt-1 text-2xl">{preview.movieTitle}</h2>
              <p className="mt-1 text-sm text-ink-muted">
                {preview.hallName} · {new Date(preview.sessionStartTime).toLocaleString("uk-UA")}
              </p>
              <p className="mt-2">Ряд {preview.rowNumber}, місце {preview.seatNumber}</p>
              <p className="text-sm text-ink-muted">Власник: {preview.holderFullName}</p>
              {preview.validatedAt && (
                <p className="mt-2 text-sm text-accent">Уже перевірено: {new Date(preview.validatedAt).toLocaleString("uk-UA")}</p>
              )}

              {preview.status === "ACTIVE" && (
                <button
                  onClick={handleConfirm}
                  disabled={loading}
                  className="mt-4 w-full rounded-sm bg-marquee py-3 font-medium text-charcoal-950 hover:opacity-90 disabled:opacity-60"
                >
                  Пропустити
                </button>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
