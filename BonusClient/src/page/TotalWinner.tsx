import MemoDialog from "@Components/Dialog";
import WinnerItem from "@Components/WinnerItem";
import { RecordType, RecordsType } from "@Type/Record";
import { useAuth } from "@Zustand/useAuth";
import { useRecords } from "@Zustand/useRecords";
import dayjs from "dayjs";
import { AnimatePresence, motion } from "framer-motion";
import { useEffect, useMemo, useState } from "react";

import BG from "@Assets/bonusWinnerBg.png";

const TotalWinner = () => {
  const { webSite } = useAuth();
  const {
    records,
    currentRecord,
    setRecordsWithBounsType,
    setAllRecord,
    recordA,
    recordB,
    recordCR,
  } = useRecords();

  const [recordsArr, setRecordsArr] = useState<RecordType[]>([]);
  const [newRecordsArr, setNewRecordsArr] = useState<RecordType[]>([]);
  const [newestContent, setNewestContent] = useState<string | null>(null);
  const [isVisible, setIsVisible] = useState(true);
  const [isDialogVisible, setIsDialogVisible] = useState(false);
  const [timer, setTimer] = useState<NodeJS.Timeout | null>(null);

  const [isProcessing, setIsProcessing] = useState(false);

  const recordsObj: RecordsType | null = useMemo(() => {
    if (records) {
      const _records = JSON.parse(records);
      return _records[webSite];
    }
    return null;
  }, [records, webSite]);

  useEffect(() => {
    if (recordsObj) {
      setRecordsWithBounsType(recordsObj);

      const allRecords = [
        ...(recordsObj.WinAList || []),
        ...(recordsObj.WinBList || []),
        ...(recordsObj.WinCRList || []),
      ];

      allRecords.sort((a: RecordType, b: RecordType) => {
        const dateA = dayjs(a.CreateTime);
        const dateB = dayjs(b.CreateTime);
        return dateB.diff(dateA);
      });

      setAllRecord(allRecords);
      setNewRecordsArr(allRecords);
    }
  }, [recordsObj]);

  useEffect(() => {
    setIsVisible(false);
    setIsDialogVisible(true);
    setNewestContent(newRecordsArr[0]?.Message ?? "");
    setTimeout(() => {
      setRecordsArr(newRecordsArr);
      setIsVisible(true);
    }, 500); // 500ms for fade-out effect
    setTimeout(() => {
      setIsDialogVisible(false);
    }, 3500);
  }, [newRecordsArr]);

  useEffect(() => {
    if (isProcessing) {
      return;
    }

    if (timer) {
      clearTimeout(timer);
    }

    if (currentRecord === "ALL") {
      setIsVisible(false);
      setTimeout(() => {
        setRecordsArr(newRecordsArr);
        setIsVisible(true);
      }, 500);
    } else {
      const newTimer = setTimeout(() => {
        setRecordsArr(newRecordsArr || []);
      }, 15000);

      setTimer(newTimer);
      switch (currentRecord) {
        case "A":
          setRecordsArr(recordA || []);
          break;
        case "B":
          setRecordsArr(recordB || []);
          break;
        case "CR":
          setRecordsArr(recordCR || []);
          break;
        default:
          break;
      }
    }

    setIsProcessing(true);
    setTimeout(() => {
      setIsProcessing(false);
    }, 500);

    return () => {
      if (timer) {
        clearTimeout(timer);
      }
    };
  }, [currentRecord]);

  return (
    <div
      style={{
        backgroundImage: `url(${BG})`,
        backgroundSize: "100% 100%",
        backgroundPosition: "center",
        backgroundRepeat: "no-repeat",
        objectFit: "contain",
        marginRight: "150px",
        padding: "18px",
      }}
    >
      <motion.div
        className="flex flex-col gap-1 items-center justify-center flex-1 "
        initial={{ opacity: 1 }}
        animate={{ opacity: isVisible ? 1 : 0 }}
        transition={{ duration: 0.15 }}
      >
        <div className="max-h-[600px] overflow-y-auto pt-3 p-6">
          {recordsArr.length === 0 ? (
            <div className="text-lg text-gray-400">No winner</div>
          ) : (
            <AnimatePresence>
              {recordsArr.map((record: RecordType, index: number) => (
                <motion.div
                  key={`${record.CreateTime}_${index}`}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  transition={{ duration: 0.5 }}
                >
                  <WinnerItem
                    key={`${record.CreateTime}_${index}`}
                    index={index}
                    record={record}
                  />
                </motion.div>
              ))}
            </AnimatePresence>
          )}
        </div>
      </motion.div>
      <MemoDialog isOpen={isDialogVisible} content={newestContent || ""} />
    </div>
  );
};

export default TotalWinner;
