import { httpClient } from "./httpClient";

export interface Promotion {
  id: string;
  code: string;
  discountType: "PERCENT" | "FIXED";
  discountValue: number;
  expiresAt: string;
  usageLimit: number | null;
  usageCount: number;
}

export const promotionsApi = {
  getPromotions: () => httpClient.get<Promotion[]>("/promotions").then((r) => r.data),
  createPromotion: (data: { code: string; discountType: string; discountValue: number; expiresAt: string; usageLimit: number | null }) =>
    httpClient.post("/promotions", data)
};
