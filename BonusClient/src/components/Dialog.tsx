import { AnimatePresence, motion } from "framer-motion";
import { memo, useMemo } from "react";

import WINNER_PNG from "@Assets/winner.png";
// import ConfettiExplosion from "react-confetti-explosion";

interface Props {
  isOpen: boolean;
  content: string;
}

// const source: CSSProperties = {
//   position: "fixed",
//   right: "50%",
//   left: "50%",
//   top: "25%",
// };

// const bigExplodeProps = {
//   force: 0.7,
//   duration: 4000,
//   particleCount: 150,
// };

const Dialog = ({ isOpen, content }: Props) => {
  const open = useMemo(() => isOpen, [isOpen]);

  return (
    // <Fragment>
    //   {open && (
    //     <div style={source}>
    //       <ConfettiExplosion {...bigExplodeProps} />
    //     </div>
    //   )}
    <AnimatePresence>
      {open && (
        <motion.div
          className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-80"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.3 }}
        >
          <motion.div
            className="rounded-lg p-6 flex justify-center items-center flex-col gap-5 relative"
            initial={{ scale: 0.5, opacity: 0, y: 200 }}
            animate={{ scale: 1, opacity: 1, y: -100 }}
            exit={{ scale: 0.5, opacity: 0, y: -300 }}
            transition={{ duration: 0.55 }}
          >
            <img
              src={WINNER_PNG}
              alt="winner"
              className="w-[1000px] h-[1000px]"
            />
            <p className="text-lg text-white absolute top-[70%] left-1/2 transform -translate-x-1/2">
              {content}
            </p>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
    // </Fragment>
  );
};

const MemoDialog = memo(Dialog);

export default MemoDialog;
