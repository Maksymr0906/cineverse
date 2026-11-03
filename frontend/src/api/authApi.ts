import { httpClient } from "./httpClient";
import type { AuthUser } from "@/types";

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

export const authApi = {
  register: (data: { firstName: string; lastName: string; email: string; phone: string; password: string }) =>
    httpClient.post("/auth/register", data),

  confirmEmail: (token: string) => httpClient.get("/auth/confirm-email", { params: { token } }),

  login: (data: { email: string; password: string }) =>
    httpClient.post<LoginResponse>("/auth/login", data).then((r) => r.data),

  forgotPassword: (email: string) => httpClient.post("/auth/forgot-password", { email }),

  resetPassword: (token: string, newPassword: string) =>
    httpClient.post("/auth/reset-password", { token, newPassword }),

  refresh: () => httpClient.post<LoginResponse>("/auth/refresh").then((r) => r.data),

  logout: () => httpClient.post("/auth/logout")
};
