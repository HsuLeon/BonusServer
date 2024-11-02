import { create } from "zustand";

type TotalBet = {
  totalBet_A: number;
  totalBet_B: number;
  totalBet_CR: number;
};

type PrevTotalBet = {
  prevTotalBet_A: number;
  prevTotalBet_B: number;
  prevTotalBet_CR: number;
};

type Bouns = TotalBet &
  PrevTotalBet & {
    setTotalBet: (totalBet: TotalBet) => void;
  };

export const useBouns = create<Bouns>((set) => ({
  prevTotalBet_A: 0,
  prevTotalBet_B: 0,
  prevTotalBet_CR: 0,
  totalBet_A: 0,
  totalBet_B: 0,
  totalBet_CR: 0,
  setTotalBet: (totalBet) =>
    set((state) => {
      if (!state.prevTotalBet_A) {
        return {
          prevTotalBet_A: totalBet.totalBet_A,
          prevTotalBet_B: totalBet.totalBet_B,
          prevTotalBet_CR: totalBet.totalBet_CR,
          ...totalBet,
        };
      } else {
        return {
          prevTotalBet_A: state.totalBet_A,
          prevTotalBet_B: state.totalBet_B,
          prevTotalBet_CR: state.totalBet_CR,
          ...totalBet,
        };
      }
    }),
}));
