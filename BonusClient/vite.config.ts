import react from "@vitejs/plugin-react";
import path from "path";
import { defineConfig } from "vite";

function resolve(dir: string) {
  return path.join(__dirname, dir);
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  base: "/",
  resolve: {
    alias: {
      "@Components": resolve("src/components"),
      "@Utils": resolve("src/utils"),
      "@Api": resolve("src/api"),
      "@Page": resolve("src/page"),
      "@Type": resolve("src/type"),
      "@Zustand": resolve("src/zustand"),
      "@Assets": resolve("src/assets"),
    },
  },
});
