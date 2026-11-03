import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { cinemasApi } from "@/api/cinemasApi";
import AdminNav from "@/components/AdminNav";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { Cinema, Hall } from "@/types";

const HALL_TYPES = ["STANDARD", "IMAX", "VIP", "THREE_D", "FOUR_DX"];
const SEAT_TYPES = ["VIP", "LOVE_SEAT", "DISABLED"] as const;
type SpecialSeatType = (typeof SEAT_TYPES)[number];

const SEAT_TYPE_LABELS: Record<SpecialSeatType, string> = {
  VIP: "VIP",
  LOVE_SEAT: "Для двох",
  DISABLED: "Для МГН"
};
const SEAT_TYPE_CLASSES: Record<SpecialSeatType, string> = {
  VIP: "bg-marquee text-charcoal-950",
  LOVE_SEAT: "bg-pink-500 text-charcoal-950",
  DISABLED: "bg-sky-500 text-charcoal-950"
};

const emptyCinemaForm = { name: "", address: "", city: "", phone: "" };
// Побудова розсадки (Rows/SeatsPerRow/особливі місця) лишається доступною лише при створенні
// залу — так само, як на бекенді: після створення можна редагувати тільки Name/Type.
const emptyHallForm = { cinemaId: "", name: "", type: "STANDARD", rows: 8, seatsPerRow: 10 };

/** Керування кінотеатрами й залами (кабінет адміністратора, п.2.5 ТЗ: "CRUD-операції для
 * кінотеатрів, залів"). Раніше було доступне лише через API напряму — ця сторінка додає
 * створення кінотеатру/залу (тонка форма над уже наявними ендпоінтами) і, головне,
 * редагування назви/типу вже існуючого залу. */
export default function AdminCinemasPage() {
  const [cinemas, setCinemas] = useState<Cinema[]>([]);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);

  const [cinemaForm, setCinemaForm] = useState(emptyCinemaForm);
  const [cinemaError, setCinemaError] = useState<string | null>(null);
  const [cinemaSubmitting, setCinemaSubmitting] = useState(false);

  const [hallForm, setHallForm] = useState(emptyHallForm);
  const [hallError, setHallError] = useState<string | null>(null);
  const [hallSubmitting, setHallSubmitting] = useState(false);

  // Розмітка "особливих" місць для форми створення залу: ключ "рядок-місце" -> тип.
  // Відсутність запису в мапі означає STANDARD (місце "не пофарбоване").
  const [paintType, setPaintType] = useState<SpecialSeatType>("VIP");
  const [specialSeats, setSpecialSeats] = useState<Record<string, SpecialSeatType>>({});

  const updateHallDimensions = (patch: Partial<typeof hallForm>) => {
    // Rows/SeatsPerRow змінились — стара розмітка може вказувати на координати поза межами
    // нової сітки, тож скидаємо її, а не намагаємось "обрізати" частково.
    setSpecialSeats({});
    setHallForm((f) => ({ ...f, ...patch }));
  };

  const toggleSpecialSeat = (row: number, seat: number) => {
    const key = `${row}-${seat}`;
    setSpecialSeats((prev) => {
      const next = { ...prev };
      if (next[key] === paintType) delete next[key]; // повторний клік тим самим типом -> назад у STANDARD
      else next[key] = paintType;
      return next;
    });
  };

  const [editingHall, setEditingHall] = useState<Hall | null>(null);
  const [editForm, setEditForm] = useState({ name: "", type: "STANDARD" });
  const [editError, setEditError] = useState<string | null>(null);
  const [editSubmitting, setEditSubmitting] = useState(false);

  const loadCinemas = () =>
    cinemasApi
      .getCinemas()
      .then(setCinemas)
      .catch(() => setListError("Не вдалося завантажити список кінотеатрів."));

  useEffect(() => {
    loadCinemas().finally(() => setLoading(false));
  }, []);

  const handleCreateCinema = async (e: FormEvent) => {
    e.preventDefault();
    setCinemaSubmitting(true);
    setCinemaError(null);
    try {
      await cinemasApi.createCinema(cinemaForm);
      setCinemaForm(emptyCinemaForm);
      await loadCinemas();
    } catch (err) {
      setCinemaError(extractApiErrorMessage(err, "Не вдалося створити кінотеатр."));
    } finally {
      setCinemaSubmitting(false);
    }
  };

  const handleCreateHall = async (e: FormEvent) => {
    e.preventDefault();
    setHallSubmitting(true);
    setHallError(null);
    try {
      const specialSeatsPayload = Object.entries(specialSeats).map(([key, seatType]) => {
        const [rowNumber, seatNumber] = key.split("-").map(Number);
        return { rowNumber, seatNumber, seatType };
      });
      await cinemasApi.createHall({ ...hallForm, specialSeats: specialSeatsPayload });
      setHallForm({ ...emptyHallForm, cinemaId: hallForm.cinemaId });
      setSpecialSeats({});
      await loadCinemas();
    } catch (err) {
      setHallError(extractApiErrorMessage(err, "Не вдалося створити зал."));
    } finally {
      setHallSubmitting(false);
    }
  };

  const startEditing = (hall: Hall) => {
    setEditingHall(hall);
    setEditForm({ name: hall.name, type: hall.type });
    setEditError(null);
  };

  const cancelEditing = () => {
    setEditingHall(null);
    setEditError(null);
  };

  const handleSaveHall = async (e: FormEvent) => {
    e.preventDefault();
    if (!editingHall) return;
    setEditSubmitting(true);
    setEditError(null);
    try {
      await cinemasApi.updateHall(editingHall.id, editForm);
      setEditingHall(null);
      await loadCinemas();
    } catch (err) {
      setEditError(extractApiErrorMessage(err, "Не вдалося зберегти зал."));
    } finally {
      setEditSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">Адмінка</h1>
      <div className="mt-4"><AdminNav /></div>

      <div className="mt-8 grid gap-10 sm:grid-cols-[1fr_320px]">
        <div>
          <h2 className="text-lg font-medium">Кінотеатри й зали</h2>
          {listError && <p className="mt-2 text-sm text-accent">{listError}</p>}
          {loading && <p className="mt-2 text-sm text-ink-muted">Завантаження…</p>}

          <div className="mt-3 flex flex-col gap-6">
            {cinemas.map((cinema) => (
              <div key={cinema.id}>
                <p className="font-medium">{cinema.name} <span className="text-xs text-ink-muted">· {cinema.city}</span></p>
                <div className="mt-2 divide-y divide-charcoal-700 border-t border-charcoal-700">
                  {cinema.halls.map((hall) =>
                    editingHall?.id === hall.id ? (
                      <form key={hall.id} onSubmit={handleSaveHall} className="flex flex-wrap items-center gap-2 py-3">
                        <input required value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                          className="w-32 rounded-sm border border-charcoal-600 bg-charcoal-900 px-2 py-1 text-sm" />
                        <select value={editForm.type} onChange={(e) => setEditForm({ ...editForm, type: e.target.value })}
                          className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-2 py-1 text-sm">
                          {HALL_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                        </select>
                        <button type="submit" disabled={editSubmitting} className="text-sm text-marquee hover:underline disabled:opacity-60">
                          {editSubmitting ? "Зберігаємо…" : "Зберегти"}
                        </button>
                        <button type="button" onClick={cancelEditing} className="text-sm text-ink-muted hover:text-ink">Скасувати</button>
                        {editError && <p className="w-full text-xs text-accent">{editError}</p>}
                      </form>
                    ) : (
                      <div key={hall.id} className="flex items-center justify-between py-3">
                        <div>
                          <p>{hall.name}</p>
                          <p className="text-xs text-ink-muted">{hall.type} · {hall.capacity} місць</p>
                        </div>
                        <button onClick={() => startEditing(hall)} className="text-sm text-marquee hover:underline">Редагувати</button>
                      </div>
                    )
                  )}
                  {cinema.halls.length === 0 && <p className="py-3 text-sm text-ink-muted">Залів ще немає.</p>}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="flex flex-col gap-8">
          <form onSubmit={handleCreateCinema} className="flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
            <h2 className="text-lg font-medium">Додати кінотеатр</h2>
            <input required placeholder="Назва" value={cinemaForm.name} onChange={(e) => setCinemaForm({ ...cinemaForm, name: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            <input required placeholder="Місто" value={cinemaForm.city} onChange={(e) => setCinemaForm({ ...cinemaForm, city: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            <input required placeholder="Адреса" value={cinemaForm.address} onChange={(e) => setCinemaForm({ ...cinemaForm, address: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            <input placeholder="Телефон" value={cinemaForm.phone} onChange={(e) => setCinemaForm({ ...cinemaForm, phone: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            {cinemaError && <p className="text-sm text-accent">{cinemaError}</p>}
            <button type="submit" disabled={cinemaSubmitting} className="mt-1 rounded-sm bg-accent py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
              {cinemaSubmitting ? "Зберігаємо…" : "Додати кінотеатр"}
            </button>
          </form>

          <form onSubmit={handleCreateHall} className="flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
            <h2 className="text-lg font-medium">Додати зал</h2>
            <select required value={hallForm.cinemaId} onChange={(e) => setHallForm({ ...hallForm, cinemaId: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
              <option value="">Оберіть кінотеатр</option>
              {cinemas.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <input required placeholder="Назва залу" value={hallForm.name} onChange={(e) => setHallForm({ ...hallForm, name: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            <select value={hallForm.type} onChange={(e) => setHallForm({ ...hallForm, type: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
              {HALL_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <div className="grid grid-cols-2 gap-2">
              <input type="number" min={1} max={50} required placeholder="Рядів" value={hallForm.rows}
                onChange={(e) => updateHallDimensions({ rows: Number(e.target.value) })}
                className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
              <input type="number" min={1} max={50} required placeholder="Місць у ряду" value={hallForm.seatsPerRow}
                onChange={(e) => updateHallDimensions({ seatsPerRow: Number(e.target.value) })}
                className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            </div>

            <div>
              <div className="mb-1.5 flex items-center justify-between">
                <p className="text-xs text-ink-muted">Розмітка місць</p>
                {Object.keys(specialSeats).length > 0 && (
                  <button type="button" onClick={() => setSpecialSeats({})} className="text-xs text-ink-muted hover:text-ink">
                    Скинути розмітку
                  </button>
                )}
              </div>
              <div className="mb-2 flex flex-wrap gap-1.5">
                {SEAT_TYPES.map((t) => (
                  <button type="button" key={t} onClick={() => setPaintType(t)}
                    className={`rounded-sm px-2 py-1 text-xs ${paintType === t ? SEAT_TYPE_CLASSES[t] : "border border-charcoal-600 text-ink-muted"}`}>
                    {SEAT_TYPE_LABELS[t]}
                  </button>
                ))}
              </div>
              <p className="mb-1.5 text-xs text-ink-muted">
                Оберіть тип вище, тоді клікайте на місця нижче, щоб їх позначити. Непозначені
                місця лишаться STANDARD. Повторний клік тим самим типом знімає позначку.
              </p>
              <div className="max-h-64 overflow-auto rounded-sm border border-charcoal-700 p-2">
                <div className="flex flex-col gap-1">
                  {Array.from({ length: hallForm.rows }, (_, i) => i + 1).map((row) => (
                    <div key={row} className="grid gap-1" style={{ gridTemplateColumns: `repeat(${hallForm.seatsPerRow}, minmax(0, 1fr))` }}>
                      {Array.from({ length: hallForm.seatsPerRow }, (_, i) => i + 1).map((seat) => {
                        const type = specialSeats[`${row}-${seat}`];
                        return (
                          <button type="button" key={seat} onClick={() => toggleSpecialSeat(row, seat)}
                            title={`Ряд ${row}, місце ${seat}${type ? ` — ${SEAT_TYPE_LABELS[type]}` : ""}`}
                            className={`aspect-square min-w-[1.25rem] rounded-sm text-[9px] leading-none ${type ? SEAT_TYPE_CLASSES[type] : "bg-charcoal-800 text-ink-muted hover:bg-charcoal-700"}`}>
                            {seat}
                          </button>
                        );
                      })}
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {hallError && <p className="text-sm text-accent">{hallError}</p>}
            <button type="submit" disabled={hallSubmitting} className="mt-1 rounded-sm bg-accent py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
              {hallSubmitting ? "Зберігаємо…" : "Додати зал"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
