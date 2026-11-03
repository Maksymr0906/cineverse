import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { authApi } from "@/api/authApi";
import { setAccessToken } from "@/api/httpClient";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { AuthUser } from "@/types";

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  status: "idle" | "loading" | "authenticated" | "unauthenticated";
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  accessToken: null,
  status: "idle",
  error: null
};

export const login = createAsyncThunk(
  "auth/login",
  async (data: { email: string; password: string }, { rejectWithValue }) => {
    try {
      return await authApi.login(data);
    } catch (err) {
      const message = extractApiErrorMessage(err, "Не вдалося увійти. Перевірте email і пароль.");
      return rejectWithValue(message);
    }
  }
);

export const logout = createAsyncThunk("auth/logout", async () => {
  await authApi.logout();
});

/** Викликається один раз при завантаженні застосунку: пробує "мовчки" оновити сесію
 * через HttpOnly refresh-cookie, щоб не змушувати користувача входити заново після F5. */
export const restoreSession = createAsyncThunk("auth/restore", async (_, { rejectWithValue }) => {
  try {
    return await authApi.refresh();
  } catch {
    return rejectWithValue(null);
  }
});

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    /** Викликається з httpClient при провалі silent-refresh (Етап 8, виправлення після аудиту):
     * раніше axios-інтерцептор сам скидав access-токен у пам'яті (setAccessToken(null)), але
     * НІКОЛИ не повідомляв про це Redux — стан лишався "authenticated", і ProtectedRoute/Navbar
     * продовжували показувати користувача залогіненим, хоча кожен наступний запит тихо падав
     * би з 401 без жодного повідомлення чи редіректу на логін. */
    sessionExpired: (state) => {
      state.status = "unauthenticated";
      state.user = null;
      state.accessToken = null;
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.status = "loading";
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.status = "authenticated";
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
        setAccessToken(action.payload.accessToken);
      })
      .addCase(login.rejected, (state, action) => {
        state.status = "unauthenticated";
        state.error = (action.payload as string) ?? "Не вдалося увійти. Перевірте email і пароль.";
      })
      .addCase(restoreSession.fulfilled, (state, action) => {
        state.status = "authenticated";
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
        setAccessToken(action.payload.accessToken);
      })
      .addCase(restoreSession.rejected, (state) => {
        state.status = "unauthenticated";
      })
      .addCase(logout.fulfilled, (state) => {
        state.status = "unauthenticated";
        state.user = null;
        state.accessToken = null;
        setAccessToken(null);
      })
      .addCase(logout.rejected, (state) => {
        // Виправлення після аудиту: раніше не було цього case — logout thunk завжди резолвиться
        // (fulfilled), навіть якщо запит /auth/logout впав мережево, тож UI мовчки лишався
        // б у стані "authenticated". Тепер стан скидається незалежно від успіху мережевого запиту:
        // з точки зору фронтенду важливо, щоб користувач локально вважався вийшлим.
        state.status = "unauthenticated";
        state.user = null;
        state.accessToken = null;
        setAccessToken(null);
      });
  }
});

export default authSlice.reducer;
export const { sessionExpired } = authSlice.actions;
