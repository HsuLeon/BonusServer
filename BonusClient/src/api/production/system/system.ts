import AXIOS from "@Api/AXIOS";
import { SysteNotifymResponse } from "@Type/api/System";
import { AxiosResponse } from "axios";
import SystemProtocol from "./protocol";

export async function notify(): Promise<SysteNotifymResponse> {
  try {
    const res: AxiosResponse<SysteNotifymResponse> = await AXIOS.get(
      SystemProtocol.NOTIFY
    );
    return res.data;
  } catch (e) {
    throw new Error(`${SystemProtocol.NOTIFY}, ${e as string}`);
  }
}
