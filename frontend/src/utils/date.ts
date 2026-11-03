/**
 * Формат YYYY-MM-DD за ЛОКАЛЬНИМ часом браузера, а не UTC.
 *
 * Виправлення після аудиту: `date.toISOString().slice(0, 10)` завжди повертає дату в UTC.
 * Для часового поясу України (UTC+2/+3) у перші 2-3 години доби це фактично давало ВЧОРАШНЮ
 * календарну дату — вибір "сьогодні" на сторінці фільму та дефолтна дата прем'єри в адмінці
 * могли показувати не той день. Цей хелпер рахує рік/місяць/день так, як їх бачить сам браузер
 * (тобто за системним часовим поясом користувача), без проміжної конвертації в UTC.
 */
export function toLocalDateString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
