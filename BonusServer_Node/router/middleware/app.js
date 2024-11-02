import cors from "cors";
import express from "express";
import formData from "express-form-data";

export default (app) => {
  app.use(cors({ origin: true }));
  app.use(
    express.raw({
      type: "application/octet-stream",
      limit: "200mb",
    })
  );
  app.use(express.json({ limit: "200mb" }));
  app.use(express.urlencoded({ limit: "200mb", extended: true }));
  app.use(formData.parse());
};
