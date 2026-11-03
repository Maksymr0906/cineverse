import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { BrowserRouter } from "react-router-dom";
import { store } from "@/app/store";
import { restoreSession } from "@/features/auth/authSlice";
import App from "@/App";
import "./styles/index.css";

// Пробуємо "мовчки" відновити сесію через HttpOnly refresh-cookie одразу при завантаженні,
// щоб користувач не бачив стан "не увійшли" на частку секунди після оновлення сторінки (F5).
store.dispatch(restoreSession());

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <Provider store={store}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </Provider>
  </React.StrictMode>
);
