export type LoginRequest = {
  machineName: string;
  scoreScale: number;
};

export type LoginResponse = {
  token: string;
  webSite: string;
  ruleId: number;
};
