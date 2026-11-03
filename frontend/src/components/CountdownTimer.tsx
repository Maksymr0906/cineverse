import { useEffect, useState } from "react";

/** Зворотний відлік до ExpiresAt бронювання (10 хв, Етап 5). Викликає onExpire один раз при досягненні нуля. */
export default function CountdownTimer({ expiresAt, onExpire }: { expiresAt: string; onExpire?: () => void }) {
  const [secondsLeft, setSecondsLeft] = useState(() => Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000)));

  useEffect(() => {
    if (secondsLeft <= 0) {
      onExpire?.();
      return;
    }
    const timer = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          clearInterval(timer);
          onExpire?.();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [expiresAt]); // eslint-disable-line react-hooks/exhaustive-deps

  const minutes = Math.floor(secondsLeft / 60);
  const seconds = secondsLeft % 60;
  const isUrgent = secondsLeft < 60;

  return (
    <span className={`font-mono text-lg ${isUrgent ? "text-accent" : "text-marquee"}`}>
      {minutes}:{seconds.toString().padStart(2, "0")}
    </span>
  );
}
