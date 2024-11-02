import { notify } from "@Api/production/system/system";
import { spin } from "@Api/test/spin/spin";
import Title from "@Components/Title";
import { SpinRequest } from "@Type/api/Spin";
import { useBouns } from "@Zustand/useBonus";
import { useRecords } from "@Zustand/useRecords";
import { useEffect } from "react";
import TotalBonus from "./TotalBonus";
import TotalWinner from "./TotalWinner";

import BG from "../assets/bg.jpg";

const BonusPage = () => {
  const { setTotalBet } = useBouns();
  const { setRecords } = useRecords();

  const spinHandler = async () => {
    const request1: SpinRequest = {
      userAccount: "Jeff",
      bonusType: 1,
      totalBet: Math.floor(Math.random() * 150) + 1,
      totalWin: Math.floor(Math.random() * 10) + 1,
      winA: 1,
      winB: 1,
    };

    const request2: SpinRequest = {
      userAccount: "Jeff",
      bonusType: 2,
      totalBet: Math.floor(Math.random() * 150) + 1,
      totalWin: Math.floor(Math.random() * 20) + 1,
      winA: 1,
      winB: 1,
    };

    const requests = [request1, request2];
    const randomIndex = Math.floor(Math.random() * requests.length);
    const selectedRequest = requests[randomIndex];

    await spin(selectedRequest);
  };

  const getRecordsPer10Sec = async () => {
    const system = await notify();

    const { records, ...totalBet } = system;
    setRecords(records);
    setTotalBet(totalBet);
  };

  useEffect(() => {
    getRecordsPer10Sec();

    setInterval(() => {
      spinHandler();
    }, 3000);

    const interval = setInterval(() => {
      getRecordsPer10Sec();
    }, 10000);

    return () => clearInterval(interval);
  }, []);

  return (
    <div
      className="min-h-[100vh] flex flex-col justify-start items-center p-2 gap-10"
      style={{
        backgroundImage: `url(${BG})`,
        backgroundSize: "cover",
        objectFit: "cover",
        backgroundPosition: "center",
        backgroundRepeat: "no-repeat",
      }}
    >
      <Title />
      <div className="flex flex-col lg:flex-row w-full gap-10 lg:gap-0">
        <TotalBonus />
        <TotalWinner />
      </div>
    </div>
  );
};

export default BonusPage;
