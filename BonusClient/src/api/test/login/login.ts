import AXIOS from "@Api/AXIOS";
import { LoginRequest, LoginResponse } from "@Type/api/Login";
import { AxiosResponse } from "axios";
import LoginProtocol from "./protocol";

export async function login(req: LoginRequest): Promise<LoginResponse> {
  try {
    const res = await AXIOS.post<LoginRequest, AxiosResponse<LoginResponse>>(
      LoginProtocol.TEST_LOGIN,
      req
    );
    return res.data;
  } catch (e) {
    throw new Error(`${LoginProtocol.TEST_LOGIN}, ${e as string}`);
  }
}
