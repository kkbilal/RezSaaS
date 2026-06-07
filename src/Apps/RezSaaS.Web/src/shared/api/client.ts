import createClient from "openapi-fetch";
import type { paths } from "./rezsaas-api.generated";

export const apiClient = createClient<paths>({
  baseUrl: "",
  credentials: "include"
});
