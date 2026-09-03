import axios from "axios";
import { resolveApiBaseUrl } from "@/utils/apiBase.js";

/** Anonymous menu/order calls — never redirect to login. */
export const publicHttp = axios.create({
  baseURL: resolveApiBaseUrl(),
  timeout: 30000,
});
