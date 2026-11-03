import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import type { SeatStatus } from "@/types";

export interface SeatStatusChange {
  seatId: string;
  status: SeatStatus;
}

/**
 * Підключення до HallHub (Етап 5) для конкретного сеансу: приєднується до групи
 * "session-{sessionId}" і повертає останню отриману пачку змін статусів місць,
 * щоб SeatSelectionPage міг оновити локальний стан без перезавантаження сторінки.
 */
export function useSeatStatusUpdates(sessionId: string | undefined) {
  const [lastUpdate, setLastUpdate] = useState<SeatStatusChange[] | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!sessionId) return;

    const hubUrl = import.meta.env.VITE_HUB_URL ?? "/hubs/hall";
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on("seatsUpdated", (changes: SeatStatusChange[]) => {
      setLastUpdate(changes);
    });

    connection
      .start()
      .then(() => connection.invoke("JoinSessionGroup", sessionId))
      .catch((err) => console.error("Не вдалося підключитись до HallHub:", err));

    connectionRef.current = connection;

    return () => {
      connection.invoke("LeaveSessionGroup", sessionId).catch(() => {});
      connection.stop();
    };
  }, [sessionId]);

  return lastUpdate;
}
