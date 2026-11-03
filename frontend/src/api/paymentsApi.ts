import { httpClient } from "./httpClient";

export const paymentsApi = {
  initiate: (bookingId: string) =>
    httpClient.post<{ paymentId: string; redirectUrl: string }>("/payments/initiate", { bookingId }).then((r) => r.data),

  mockComplete: (transactionId: string, success: boolean) =>
    httpClient.post(`/payments/mock/${transactionId}/complete`, { success }),

  confirmCash: (bookingId: string) => httpClient.post(`/payments/${bookingId}/cash`)
};
