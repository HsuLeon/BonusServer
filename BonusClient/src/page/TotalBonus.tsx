import BonusItem, { BonusItemType } from "@Components/BonusItem";
import { useBouns } from "@Zustand/useBonus";

const TotalBonus = () => {
  const {
    totalBet_A,
    totalBet_B,
    totalBet_CR,
    prevTotalBet_A,
    prevTotalBet_B,
    prevTotalBet_CR,
  } = useBouns();

  const BonusItems: BonusItemType[] = [
    {
      name: "斯洛",
      type: "MEGA",
      amount: totalBet_A,
      prevAmount: prevTotalBet_A,
    },
    {
      name: "斯洛",
      type: "MINI",
      amount: totalBet_B,
      prevAmount: prevTotalBet_B,
    },
    {
      name: "CR 無珠",
      type: "MEGA",
      amount: totalBet_CR,
      prevAmount: prevTotalBet_CR,
    },
  ];

  return (
    <div className="flex-1 flex flex-col items-center justify-center gap-5">
      {BonusItems.map((item, index) => (
        <BonusItem key={index} {...item} />
      ))}
    </div>
  );
};

export default TotalBonus;
