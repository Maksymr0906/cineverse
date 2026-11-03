import { useEffect, useRef, useState } from "react";
import jsQR from "jsqr";

interface QrScannerProps {
  /** Викликається РІВНО ОДИН РАЗ на успішно розпізнаний код — сканування призупиняється
   * (не спамить повторними подіями на той самий кадр), поки батьківський компонент
   * не змонтує/активує сканер знову (напр. після показу результату). */
  onDetected: (code: string) => void;
  active: boolean;
}

/**
 * Базовий сканер QR-кодів через вебкамеру (jsQR) — п.2.2 ТЗ: кабінет контролера повинен
 * показувати відеопотік, а не приймати код лише текстовим полем. Працює як з вебкамери
 * ноутбука, так і з камери телефону (facingMode: "environment" — тилова камера, якщо є).
 */
export default function QrScanner({ onDetected, active }: QrScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const rafRef = useRef<number | null>(null);
  const detectedRef = useRef(false);
  const [cameraError, setCameraError] = useState<string | null>(null);

  useEffect(() => {
    if (!active) {
      stopCamera();
      return;
    }

    detectedRef.current = false;
    startCamera();
    return stopCamera;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [active]);

  async function startCamera() {
    setCameraError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      rafRef.current = requestAnimationFrame(tick);
    } catch {
      setCameraError("Не вдалося отримати доступ до камери. Дозвольте доступ у браузері або скористайтесь ручним введенням коду нижче.");
    }
  }

  function stopCamera() {
    if (rafRef.current !== null) cancelAnimationFrame(rafRef.current);
    streamRef.current?.getTracks().forEach((t) => t.stop());
    streamRef.current = null;
  }

  function tick() {
    if (detectedRef.current) return; // вже спрацювало на цьому "заході" — чекаємо на реактивацію

    const video = videoRef.current;
    const canvas = canvasRef.current;

    if (video && canvas && video.readyState === video.HAVE_ENOUGH_DATA) {
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const ctx = canvas.getContext("2d");

      if (ctx) {
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const result = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: "dontInvert" });

        if (result?.data) {
          detectedRef.current = true;
          onDetected(result.data);
          return;
        }
      }
    }

    rafRef.current = requestAnimationFrame(tick);
  }

  return (
    <div className="relative aspect-square w-full overflow-hidden rounded-sm bg-black">
      <video ref={videoRef} muted playsInline className="h-full w-full object-cover" />
      <canvas ref={canvasRef} className="hidden" />

      {/* Рамка-орієнтир — суто візуальна, не впливає на розпізнавання */}
      <div className="pointer-events-none absolute inset-8 rounded-lg border-2 border-marquee/70" />

      {cameraError && (
        <div className="absolute inset-0 flex items-center justify-center bg-charcoal-950/90 p-6 text-center text-sm text-accent">
          {cameraError}
        </div>
      )}
    </div>
  );
}
