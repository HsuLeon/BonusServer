import { create } from "zustand";

type Auth = {
  token: string;
  webSite: string;
  setWebSite: (webSite: string) => void;
  setToken: (token: string) => void;
};

export const useAuth = create<Auth>((set) => ({
  token: "",
  webSite: "",
  setWebSite: (webSite) => set({ webSite }),
  setToken: (token) => set({ token }),
}));
