import { TransferTimeType } from "@Type/TransferTime";
import dayjs from "dayjs";

export const TransferTime = (time: string | Date, type: TransferTimeType) => {
  switch (type) {
    case "Y/M/D":
      return dayjs(time).format("YYYY/MM/DD");
    case "H:M:S":
      return dayjs(time).format("HH:mm:ss");
    default:
      return "Invalid Type";
  }
};
