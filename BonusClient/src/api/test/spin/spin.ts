import AXIOS from "@Api/AXIOS";
import { SpinRequest, SpinResponse } from "@Type/api/Spin";
import { AxiosResponse } from "axios";
import SpinProtocol from "./protocol";

export async function spin(req: SpinRequest): Promise<SpinResponse> {
  try {
    const res = await AXIOS.post<SpinRequest, AxiosResponse<SpinResponse>>(
      SpinProtocol.TEST_SPIN,
      req
    );
    return res.data;
  } catch (e) {
    throw new Error(`${SpinProtocol.TEST_SPIN}, ${e as string}`);
  }
}
