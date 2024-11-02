export type SpinRequest = {
  userAccount: string;
  bonusType: number;
  totalBet: number;
  totalWin: number;
  winA: number;
  winB: number;
};

export type SpinResponse = {
  token: string;
  webSite: string;
  ruleId: number;
};
