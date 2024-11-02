export type RecordType = {
  CreateTime: string;
  LifeInMinutes: number;
  Message: string;
  WebSite: string;
  WinSpinType: number;
  Scores: number;
  MachineName: string;
  UserAccount: string;
};

export type RecordsType = {
  WinAList: RecordType[];
  WinBList: RecordType[];
  WinCRList: RecordType[];
};
