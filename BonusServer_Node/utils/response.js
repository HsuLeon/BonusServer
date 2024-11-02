export const handleError = (res, err) => {
  res.status(500).json({ error: err.message || "Unknown error occurred" });
};
