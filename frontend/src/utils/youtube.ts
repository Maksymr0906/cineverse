/**
 * Витягує YouTube video ID з довільного формату URL (watch?v=, youtu.be/, вже embed/)
 * і повертає готовий embed-URL, або null, якщо розпізнати не вдалось — тоді сторінка
 * фільму показує звичайне посилання замість зламаного <iframe>.
 */
export function toYoutubeEmbedUrl(url: string): string | null {
  if (!url) return null;

  try {
    const parsed = new URL(url);
    let videoId: string | null = null;

    if (parsed.hostname.includes("youtu.be")) {
      videoId = parsed.pathname.slice(1);
    } else if (parsed.hostname.includes("youtube.com")) {
      if (parsed.pathname === "/watch") {
        videoId = parsed.searchParams.get("v");
      } else if (parsed.pathname.startsWith("/embed/")) {
        videoId = parsed.pathname.replace("/embed/", "");
      }
    }

    return videoId ? `https://www.youtube.com/embed/${videoId}` : null;
  } catch {
    return null;
  }
}
