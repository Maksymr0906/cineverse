import { Routes, Route } from "react-router-dom";
import Navbar from "@/components/Navbar";
import ProtectedRoute from "@/components/ProtectedRoute";
import RoleRoute from "@/components/RoleRoute";

import HomePage from "@/pages/HomePage";
import MoviePage from "@/pages/MoviePage";
import LoginPage from "@/pages/LoginPage";
import RegisterPage from "@/pages/RegisterPage";
import ConfirmEmailPage from "@/pages/ConfirmEmailPage";
import ForgotPasswordPage from "@/pages/ForgotPasswordPage";
import ResetPasswordPage from "@/pages/ResetPasswordPage";
import SeatSelectionPage from "@/pages/SeatSelectionPage";
import CheckoutPage from "@/pages/CheckoutPage";
import MockPaymentPage from "@/pages/MockPaymentPage";
import TicketsPage from "@/pages/TicketsPage";
import ProfilePage from "@/pages/ProfilePage";
import CheckerPage from "@/pages/CheckerPage";
import AdminMoviesPage from "@/pages/admin/AdminMoviesPage";
import AdminSessionsPage from "@/pages/admin/AdminSessionsPage";
import AdminCinemasPage from "@/pages/admin/AdminCinemasPage";
import AdminPromotionsPage from "@/pages/admin/AdminPromotionsPage";
import AdminAnalyticsPage from "@/pages/admin/AdminAnalyticsPage";
import NotFoundPage from "@/pages/NotFoundPage";

/** Кореневий компонент з маршрутизацією. */
export default function App() {
  return (
    <>
      <Navbar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/movies/:id" element={<MoviePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/sessions/:sessionId/seats" element={<SeatSelectionPage />} />
        <Route path="/mock-payment/:transactionId" element={<MockPaymentPage />} />

        {/* Потребують входу */}
        <Route element={<ProtectedRoute />}>
          <Route path="/bookings/:id/checkout" element={<CheckoutPage />} />
          <Route path="/bookings/:id/tickets" element={<TicketsPage />} />
          <Route path="/profile" element={<ProfilePage />} />

          <Route element={<RoleRoute allowedRoles={["ADMIN", "CHECKER"]} />}>
            <Route path="/checker" element={<CheckerPage />} />
          </Route>

          <Route element={<RoleRoute allowedRoles={["ADMIN", "MANAGER"]} />}>
            <Route path="/admin/movies" element={<AdminMoviesPage />} />
            <Route path="/admin/sessions" element={<AdminSessionsPage />} />
            <Route path="/admin/promotions" element={<AdminPromotionsPage />} />
            <Route path="/admin/analytics" element={<AdminAnalyticsPage />} />
          </Route>

          {/* CreateCinema/CreateHall/UpdateHall на бекенді захищені політикою AdminOnly
              (не StaffOnly, як фільми/сеанси) — тому й тут вужче, лише ADMIN, щоб MANAGER
              не бачив форму, яка все одно впаде з 403. */}
          <Route element={<RoleRoute allowedRoles={["ADMIN"]} />}>
            <Route path="/admin/cinemas" element={<AdminCinemasPage />} />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </>
  );
}
