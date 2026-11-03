import { httpClient } from "./httpClient";

export interface Review {
  id: string;
  authorFirstName: string;
  rating: number;
  comment: string;
  isVerified: boolean;
  createdAt: string;
}

export const reviewsApi = {
  getReviews: (movieId: string) => httpClient.get<Review[]>(`/movies/${movieId}/reviews`).then((r) => r.data),
  createReview: (movieId: string, rating: number, comment: string) =>
    httpClient.post(`/movies/${movieId}/reviews`, { rating, comment })
};
