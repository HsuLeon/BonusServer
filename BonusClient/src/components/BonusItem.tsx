import { AnimatePresence, motion } from "framer-motion";
import { memo, useEffect, useState } from "react";

import BONUS_TITLE_PNG from "@Assets/bonusTitle.png";

interface BonusItemProps {
  name: string;
  type: string;
  amount: number;
  prevAmount: number;
}

export type BonusItemType = BonusItemProps;

interface DigitProps {
  digit: string;
  index: number;
}

const Digit: React.FC<DigitProps> = ({ digit, index }) => {
  return (
    <motion.div
      key={`${digit}-${index}`}
      initial={{ y: 10, opacity: 0 }}
      animate={{
        y: 0,
        opacity: 1,
      }}
      transition={{ duration: 0.05, type: "tween" }}
      className="golden-gradient-text"
    >
      <p>{digit}</p>
    </motion.div>
  );
};

const BonusItem = ({ name, type, amount, prevAmount }: BonusItemProps) => {
  const [currentAmount, setCurrentAmount] = useState(prevAmount.toFixed(2));

  useEffect(() => {
    if (amount === prevAmount) setCurrentAmount(amount.toFixed(2));
    const duration = 10000;
    const stepTime = 50;
    const totalSteps = duration / stepTime;
    const incrementPerStep = (amount - prevAmount) / totalSteps;
    let _currentAmount = prevAmount;

    const interval = setInterval(() => {
      if (
        (incrementPerStep > 0 && _currentAmount < amount) ||
        (incrementPerStep < 0 && _currentAmount > amount)
      ) {
        _currentAmount += incrementPerStep;
        setCurrentAmount(_currentAmount.toFixed(2));
      } else {
        setCurrentAmount(amount.toFixed(2));
        clearInterval(interval);
      }
    }, stepTime);

    return () => clearInterval(interval);
  }, [amount, prevAmount]);

  return (
    <div className={`flex-1 relative`}>
      {/* <div className="flex justify-center items-center">
        <div className="bg-orange-600 rounded-l-3xl p-6 lg:p-7 relative">
          <img
            src={BONUS_TITLE_PNG}
            alt="bonus-title"
            className="h-[1000px] w-[1000px] absolute top-[-5px] left-0 lg:h-12 lg:w-12"
          />
        </div>
        <div className="flex gap-3 rounded-r-3xl bg-blue-600 text-lg p-2.5 lg:text-2xl lg:p-3">
          <p>{name}</p>
          <p>{type}</p>
        </div>
      </div>
      */}
      <img
        src={BONUS_TITLE_PNG}
        alt="bonus-title"
        className="h-[180px] w-[550px] opacity-55"
      />
      <div className="flex justify-center items-center absolute top-[37.5%] right-[7.5%] transform">
        <AnimatePresence>
          {currentAmount.split("").map((digit, index) => (
            <Digit key={index} digit={digit} index={index} />
          ))}
        </AnimatePresence>
      </div>
    </div>
  );
};

const MemoBonusItem = memo(BonusItem);

export default MemoBonusItem;
