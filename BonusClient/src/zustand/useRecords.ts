import { RecordType, RecordsType } from "@Type/Record";
import { create } from "zustand";

type RecordBounsType = "A" | "B" | "CR" | "ALL";

type Records = {
  records: string;
  currentRecord: RecordBounsType;
  recordA: RecordType[] | undefined;
  recordB: RecordType[] | undefined;
  recordCR: RecordType[] | undefined;
  recordALL: RecordType[] | undefined;
  setRecords: (records: string) => void;
  setRecordsWithBounsType: (records: RecordsType) => void;
  setNextRecord: () => void;
  setAllRecord: (records: RecordType[]) => void;
};

const recordOrder: RecordBounsType[] = ["A", "B", "CR", "ALL"];

export const useRecords = create<Records>((set, get) => ({
  records: "",
  currentRecord: "ALL",
  recordA: undefined,
  recordB: undefined,
  recordCR: undefined,
  recordALL: undefined,
  setRecordsWithBounsType: (records: RecordsType) => {
    set({ recordA: records.WinAList.reverse() });
    set({ recordB: records.WinBList.reverse() });
    set({ recordCR: records.WinCRList.reverse() });
  },
  setRecords: (records) => set({ records }),
  setNextRecord: () => {
    const checkRecord = (record: RecordBounsType) => {
      if (get()[`record${record}`] && get()[`record${record}`]?.length) {
        set({ currentRecord: record });
      } else {
        checkRecord(
          recordOrder[(recordOrder.indexOf(record) + 1) % recordOrder.length]
        );
      }
    };

    if (
      (!get().recordA || !get().recordA?.length) &&
      (!get().recordB || !get().recordB?.length) &&
      (!get().recordCR || !get().recordCR?.length)
    ) {
      set({ currentRecord: "ALL" });
      return;
    }
    const currentIndex = recordOrder.indexOf(get().currentRecord);
    checkRecord(recordOrder[(currentIndex + 1) % recordOrder.length]);
  },
  setAllRecord: (records: RecordType[]) =>
    set({
      recordALL: records,
      currentRecord: "ALL" as RecordBounsType,
    }),
}));
